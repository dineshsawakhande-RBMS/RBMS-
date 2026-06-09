output "db_secret_arn" {
  value = aws_secretsmanager_secret.db.arn
}

output "jwt_secret_arn" {
  value = aws_secretsmanager_secret.jwt.arn
}

output "db_password" {
  description = "Generated DB master password (consumed by the RDS module)."
  value       = random_password.db.result
  sensitive   = true
}
