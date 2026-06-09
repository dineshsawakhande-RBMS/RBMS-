output "product_images_bucket" {
  value = aws_s3_bucket.this["product_images"].id
}

output "employee_docs_bucket" {
  value = aws_s3_bucket.this["employee_docs"].id
}

output "business_docs_bucket" {
  value = aws_s3_bucket.this["business_docs"].id
}

output "cloudfront_domain_name" {
  value = aws_cloudfront_distribution.images.domain_name
}

output "cloudfront_distribution_id" {
  value = aws_cloudfront_distribution.images.id
}
