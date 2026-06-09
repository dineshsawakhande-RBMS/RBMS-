output "cluster_name" {
  value = aws_ecs_cluster.this.name
}

output "service_name" {
  value = aws_ecs_service.api.name
}

output "alb_dns_name" {
  value = aws_lb.this.dns_name
}

output "alb_arn" {
  value = aws_lb.this.arn
}

output "ecr_repository_url" {
  value = aws_ecr_repository.api.repository_url
}

output "task_execution_role_arn" {
  value = aws_iam_role.execution.arn
}
