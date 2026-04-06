# =============================================================================
# SSM Parameter Store (Free Tier: standard parameters always free)
# =============================================================================

resource "aws_ssm_parameter" "db_connection_string" {
  name  = "/${var.project_name}/db-connection-string"
  type  = "SecureString"
  value = "Host=${aws_db_instance.main.address};Port=${aws_db_instance.main.port};Database=${var.db_name};Username=${var.db_username};Password=${random_password.db.result}"

  tags = { Name = "${var.project_name}-db-connection-string" }
}

resource "aws_ssm_parameter" "blockcypher_token" {
  name  = "/${var.project_name}/blockcypher-token"
  type  = "SecureString"
  value = var.blockcypher_token

  tags = { Name = "${var.project_name}-blockcypher-token" }
}
