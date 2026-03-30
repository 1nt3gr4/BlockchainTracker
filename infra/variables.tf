variable "aws_region" {
  description = "AWS region to deploy into"
  type        = string
  default     = "us-east-1"
}

variable "environment" {
  description = "Deployment environment"
  type        = string
  default     = "production"
}

variable "project_name" {
  description = "Project name used for resource naming"
  type        = string
  default     = "blockchain-tracker"
}

# --- Networking ---

variable "vpc_cidr" {
  description = "CIDR block for the VPC"
  type        = string
  default     = "10.0.0.0/16"
}

variable "public_subnet_cidrs" {
  description = "CIDR blocks for public subnets (need at least 2 for ALB)"
  type        = list(string)
  default     = ["10.0.1.0/24", "10.0.2.0/24"]
}

# --- RDS ---

variable "db_name" {
  description = "PostgreSQL database name"
  type        = string
  default     = "blockchain_tracker"
}

variable "db_username" {
  description = "PostgreSQL master username"
  type        = string
  default     = "blockchain"
}

variable "db_instance_class" {
  description = "RDS instance class (db.t3.micro for free tier)"
  type        = string
  default     = "db.t3.micro"
}

variable "db_allocated_storage" {
  description = "RDS allocated storage in GB (20 GB free tier)"
  type        = number
  default     = 20
}

variable "db_engine_version" {
  description = "PostgreSQL engine version"
  type        = string
  default     = "16.4"
}

# --- ECS / EC2 ---

variable "ec2_instance_type" {
  description = "EC2 instance type for ECS cluster (t3.micro for free tier)"
  type        = string
  default     = "t3.micro"
}

variable "container_port" {
  description = "Port the application container listens on"
  type        = number
  default     = 8080
}

variable "container_memory_soft" {
  description = "Soft memory limit for the container in MB"
  type        = number
  default     = 768
}

variable "container_memory_hard" {
  description = "Hard memory limit for the container in MB"
  type        = number
  default     = 900
}

variable "container_cpu" {
  description = "CPU units for the container (1024 = 1 vCPU)"
  type        = number
  default     = 896
}

# --- Application ---

variable "blockcypher_token" {
  description = "BlockCypher API token"
  type        = string
  sensitive   = true
}

variable "container_image_tag" {
  description = "Docker image tag to deploy"
  type        = string
  default     = "latest"
}
