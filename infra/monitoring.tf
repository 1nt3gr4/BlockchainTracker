# =============================================================================
# CloudWatch Log Group (Free Tier: 5 GB ingestion/month)
# =============================================================================

resource "aws_cloudwatch_log_group" "ecs" {
  name              = "/ecs/${var.project_name}"
  retention_in_days = 7 # Minimize storage costs

  tags = { Name = "${var.project_name}-logs" }
}

# =============================================================================
# CloudWatch Alarm — ECS Service has 0 running tasks
# =============================================================================

resource "aws_cloudwatch_metric_alarm" "ecs_no_running_tasks" {
  alarm_name          = "${var.project_name}-no-running-tasks"
  comparison_operator = "LessThanThreshold"
  evaluation_periods  = 2
  metric_name         = "CPUUtilization"
  namespace           = "AWS/ECS"
  period              = 300
  statistic           = "SampleCount"
  threshold           = 1
  alarm_description   = "Alert when ECS service has no running tasks"
  treat_missing_data  = "breaching"

  dimensions = {
    ClusterName = aws_ecs_cluster.main.name
    ServiceName = aws_ecs_service.app.name
  }

  tags = { Name = "${var.project_name}-no-tasks-alarm" }
}
