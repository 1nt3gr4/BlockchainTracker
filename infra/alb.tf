# =============================================================================
# Application Load Balancer (Free Tier: 750 hrs/month + 15 LCUs)
# =============================================================================

resource "aws_lb" "main" {
  name               = var.project_name
  internal           = false
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb.id]
  subnets            = aws_subnet.public[*].id

  tags = { Name = "${var.project_name}-alb" }
}

# =============================================================================
# Target Group
# =============================================================================

resource "aws_lb_target_group" "app" {
  name        = var.project_name
  port        = var.container_port
  protocol    = "HTTP"
  vpc_id      = aws_vpc.main.id
  target_type = "ip" # Required for awsvpc network mode

  health_check {
    enabled             = true
    path                = "/health"
    port                = "traffic-port"
    protocol            = "HTTP"
    interval            = 30
    timeout             = 5
    healthy_threshold   = 2
    unhealthy_threshold = 3
    matcher             = "200"
  }

  deregistration_delay = 30

  tags = { Name = "${var.project_name}-tg" }

  lifecycle {
    create_before_destroy = true
  }
}

# =============================================================================
# HTTP Listener
# =============================================================================

resource "aws_lb_listener" "http" {
  load_balancer_arn = aws_lb.main.arn
  port              = 80
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.app.arn
  }

  tags = { Name = "${var.project_name}-http-listener" }
}
