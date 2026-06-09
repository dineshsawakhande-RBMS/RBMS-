output "vpc_id" {
  description = "VPC ID."
  value       = module.networking.vpc_id
}

output "alb_dns_name" {
  description = "Public DNS name of the application load balancer."
  value       = module.ecs.alb_dns_name
}

output "ecs_cluster_name" {
  description = "ECS cluster name."
  value       = module.ecs.cluster_name
}

output "ecs_service_name" {
  description = "ECS service name (used by the deploy workflow)."
  value       = module.ecs.service_name
}

output "rds_endpoint" {
  description = "RDS PostgreSQL endpoint."
  value       = module.rds.db_endpoint
}

output "db_secret_arn" {
  description = "ARN of the database credentials secret."
  value       = module.secrets.db_secret_arn
}

output "jwt_secret_arn" {
  description = "ARN of the JWT signing key secret."
  value       = module.secrets.jwt_secret_arn
}

output "product_images_bucket" {
  description = "S3 bucket for product images."
  value       = module.s3_cloudfront.product_images_bucket
}

output "cloudfront_domain_name" {
  description = "CloudFront distribution domain serving product images."
  value       = module.s3_cloudfront.cloudfront_domain_name
}
