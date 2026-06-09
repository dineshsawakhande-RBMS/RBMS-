output "db_endpoint" {
  description = "Endpoint including port (host:port)."
  value       = aws_db_instance.this.endpoint
}

output "db_address" {
  description = "Hostname only."
  value       = aws_db_instance.this.address
}

output "db_instance_id" {
  value = aws_db_instance.this.id
}
