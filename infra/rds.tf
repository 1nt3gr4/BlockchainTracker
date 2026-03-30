# =============================================================================
# RDS PostgreSQL (Free Tier: db.t3.micro, 20 GB, Single-AZ)
# =============================================================================

resource "random_password" "db" {
  length  = 24
  special = false # avoid connection string escaping issues
}

resource "aws_db_subnet_group" "main" {
  name       = "${var.project_name}-db-subnet"
  subnet_ids = aws_subnet.public[*].id

  tags = { Name = "${var.project_name}-db-subnet-group" }
}

resource "aws_db_instance" "main" {
  identifier = var.project_name

  engine               = "postgres"
  engine_version       = var.db_engine_version
  instance_class       = var.db_instance_class
  allocated_storage    = var.db_allocated_storage
  storage_type         = "gp2"
  storage_encrypted    = true
  db_name              = var.db_name
  username             = var.db_username
  password             = random_password.db.result
  parameter_group_name = "default.postgres16"

  db_subnet_group_name   = aws_db_subnet_group.main.name
  vpc_security_group_ids = [aws_security_group.rds.id]
  publicly_accessible    = false
  multi_az               = false # Single-AZ for free tier

  backup_retention_period = 1 # Minimum, free tier includes 20 GB backup
  skip_final_snapshot     = true
  deletion_protection     = false

  tags = { Name = "${var.project_name}-rds" }
}
