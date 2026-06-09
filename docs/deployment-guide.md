# RBMS Deployment Guide

End-to-end guide for deploying the Retail Business Management System to AWS
using Terraform, ECR/ECS Fargate, RDS, S3/CloudFront, and GitHub Actions.

> Scope: production deployment. For local development use `docker compose up`
> (see `infra/docker/README.md`).

---

## 1. Prerequisites

- AWS account with admin (or equivalently scoped) access for the initial setup
- AWS CLI v2 configured locally
- Terraform >= 1.6
- Docker (for local image builds / debugging)
- A registered domain + (optionally) a Route 53 hosted zone
- GitHub repository admin access (to configure OIDC, secrets, and variables)

Choose a region (examples use `ap-south-1` / Mumbai) and stick to it.

---

## 2. Bootstrap AWS

### 2.1 Terraform remote state

Create the state bucket and lock table once (see `infra/terraform/README.md`
for the exact commands), then uncomment the `backend "s3"` block in
`infra/terraform/versions.tf`.

### 2.2 GitHub OIDC provider + deploy role

Create the GitHub OIDC identity provider and an IAM role the workflow can
assume (no long-lived keys):

1. IAM → Identity providers → Add provider → OpenID Connect
   - Provider URL: `https://token.actions.githubusercontent.com`
   - Audience: `sts.amazonaws.com`
2. Create a role with a trust policy restricted to this repo, e.g.:

   ```json
   {
     "Effect": "Allow",
     "Principal": { "Federated": "arn:aws:iam::<account-id>:oidc-provider/token.actions.githubusercontent.com" },
     "Action": "sts:AssumeRoleWithWebIdentity",
     "Condition": {
       "StringEquals": { "token.actions.githubusercontent.com:aud": "sts.amazonaws.com" },
       "StringLike": { "token.actions.githubusercontent.com:sub": "repo:<org>/<repo>:ref:refs/heads/main" }
     }
   }
   ```

3. Attach a permissions policy allowing ECR push, `ecs:UpdateService`,
   `ecs:DescribeServices`, `ecs:RunTask`, and `iam:PassRole` for the ECS task
   roles. Note the role ARN.

### 2.3 ECR repositories

The `ecs` Terraform module creates the **API** repository
(`<prefix>-api`). Create a **web** repository as well (Terraform or CLI):

```bash
aws ecr create-repository --repository-name rbms-production-web --region ap-south-1
```

---

## 3. Provision infrastructure (Terraform)

```bash
cd infra/terraform
cp terraform.tfvars.example terraform.tfvars   # edit values
terraform init
terraform plan -out tfplan
terraform apply tfplan
```

Apply ordering is handled by Terraform's dependency graph
(networking → secrets → rds → ecs; s3_cloudfront in parallel). The ECS service
starts with a placeholder image when `api_image` is empty — the deploy pipeline
replaces it on first deploy.

Capture key outputs:

```bash
terraform output
# alb_dns_name, ecs_cluster_name, ecs_service_name, rds_endpoint,
# db_secret_arn, jwt_secret_arn, cloudfront_domain_name
```

---

## 4. Configure secrets

The `secrets` module already generated and stored:

- `<prefix>/db-credentials` — username, password, dbname, port
- `<prefix>/jwt-signing-key` — issuer, audience, signinKey

The ECS task definition injects these at launch (`DB_PASSWORD`,
`JWT__SigningKey`, etc.). To rotate, update the Secrets Manager version and
force a new ECS deployment.

If the application needs SES or additional credentials, add them as Secrets
Manager entries and reference them in the task definition's `secrets` block.

---

## 5. Configure GitHub Actions

Set repository **secrets**:

| Secret         | Value                          |
| -------------- | ------------------------------ |
| `AWS_ROLE_ARN` | The OIDC deploy role ARN (2.2) |

Set repository **variables** (`vars`):

| Variable             | Example                       |
| -------------------- | ----------------------------- |
| `AWS_REGION`         | `ap-south-1`                  |
| `AWS_ACCOUNT_ID`     | `123456789012`                |
| `ECR_API_REPOSITORY` | `rbms-production-api`         |
| `ECR_WEB_REPOSITORY` | `rbms-production-web`         |
| `ECS_CLUSTER`        | `rbms-production-cluster`     |
| `ECS_API_SERVICE`    | `rbms-production-api`         |
| `API_BASE_URL`       | `https://api.yourdomain.com`  |

Configure the `production` GitHub Environment (optionally with required
reviewers) to gate deploys.

---

## 6. First deploy

Push to `main` (or run the **Deploy** workflow manually). It will:

1. Assume the AWS role via OIDC.
2. Build and push the API and Web images to ECR (tagged with the commit SHA and
   `latest`).
3. Run database migrations (see §7).
4. `aws ecs update-service --force-new-deployment` on the API service.
5. Wait for the service to reach a stable state.

Verify: `curl https://<alb_dns_name>/health` returns `200`.

---

## 7. Running migrations

The `Run database migrations` step in `deploy.yml` is a placeholder. The
recommended pattern is a one-off ECS `RunTask` using the freshly pushed image:

```bash
aws ecs run-task \
  --cluster rbms-production-cluster \
  --task-definition rbms-production-api \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[<private-subnets>],securityGroups=[<ecs-sg>],assignPublicIp=DISABLED}" \
  --overrides '{"containerOverrides":[{"name":"api","command":["dotnet","RBMS.Api.dll","--migrate"]}]}'
```

This reuses the task's secret injection (DB credentials) and runs inside the
VPC so it can reach RDS. Have the API support a `--migrate` switch (or ship a
dedicated migrations entrypoint) that runs `dotnet ef database update`.

---

## 8. DNS & CloudFront

1. **API** — point `api.yourdomain.com` at the ALB:
   - Route 53: create an A/ALIAS record to the ALB.
   - Request/validate an ACM certificate **in the ALB's region**, set
     `acm_certificate_arn` in tfvars, and re-apply to enable the HTTPS listener
     and HTTP→HTTPS redirect.
2. **Product images CDN** — the `s3_cloudfront` module outputs
   `cloudfront_domain_name`. Optionally map `cdn.yourdomain.com` to it (CNAME)
   and attach an ACM certificate **in us-east-1** (CloudFront requirement).
3. **Web** — host the Next.js container behind its own ALB/service or front it
   with CloudFront; map `app.yourdomain.com` accordingly.

---

## 9. Rollback

Images are tagged by commit SHA, so rollback is a redeploy of a known-good tag:

```bash
# 1. Identify the last good API image tag (a previous commit SHA).
# 2. Register a new task definition revision pointing at that tag, OR retag:
docker pull <registry>/rbms-production-api:<good-sha>
docker tag  <registry>/rbms-production-api:<good-sha> <registry>/rbms-production-api:latest
docker push <registry>/rbms-production-api:latest

# 3. Roll the service.
aws ecs update-service --cluster rbms-production-cluster \
  --service rbms-production-api --force-new-deployment

aws ecs wait services-stable --cluster rbms-production-cluster \
  --services rbms-production-api
```

For schema rollbacks, prefer forward-fixing migrations. If you must revert,
restore RDS from the latest automated snapshot or use point-in-time recovery
(see the production checklist). ECS keeps prior task definition revisions, so
you can also update the service back to a previous revision directly.

---

## 10. Post-deploy verification

- `GET /health` returns `200` through the ALB.
- CloudWatch log group `/ecs/<prefix>-api` shows application startup.
- ECS service shows desired == running count and a stable deployment.
- A test login succeeds end-to-end from the web app.
- Alarms (see checklist) are in `OK` state.
