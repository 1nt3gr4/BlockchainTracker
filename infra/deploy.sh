#!/usr/bin/env bash
set -euo pipefail

# =============================================================================
# deploy.sh — Build, push, and deploy BlockchainTracker to AWS ECS
# =============================================================================
#
# Prerequisites:
#   - AWS CLI configured (aws configure)
#   - Docker running
#   - Terraform applied (infra/ directory)
#
# Usage:
#   ./deploy.sh                    # Deploy with tag 'latest'
#   ./deploy.sh v1.2.3             # Deploy with specific tag

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
INFRA_DIR="$SCRIPT_DIR"

TAG="${1:-latest}"

echo "==> Reading Terraform outputs..."
ECR_URL=$(cd "$INFRA_DIR" && terraform output -raw ecr_repository_url)
CLUSTER=$(cd "$INFRA_DIR" && terraform output -raw ecs_cluster_name)
SERVICE=$(cd "$INFRA_DIR" && terraform output -raw ecs_service_name)
REGION=$(cd "$INFRA_DIR" && terraform output -raw 2>/dev/null || echo "us-east-1")

# Extract AWS account ID and region from ECR URL
AWS_ACCOUNT_ID=$(echo "$ECR_URL" | cut -d. -f1)
ECR_REGION=$(echo "$ECR_URL" | cut -d. -f4)

echo "==> Authenticating Docker with ECR..."
aws ecr get-login-password --region "$ECR_REGION" | \
  docker login --username AWS --password-stdin "$AWS_ACCOUNT_ID.dkr.ecr.$ECR_REGION.amazonaws.com"

echo "==> Building Docker image..."
docker build -t "blockchain-tracker:$TAG" "$PROJECT_ROOT"

echo "==> Tagging image..."
docker tag "blockchain-tracker:$TAG" "$ECR_URL:$TAG"

echo "==> Pushing image to ECR..."
docker push "$ECR_URL:$TAG"

echo "==> Forcing new ECS deployment..."
aws ecs update-service \
  --cluster "$CLUSTER" \
  --service "$SERVICE" \
  --force-new-deployment \
  --region "$ECR_REGION" \
  --no-cli-pager

echo "==> Deployment triggered successfully!"
echo "    Image:   $ECR_URL:$TAG"
echo "    Cluster: $CLUSTER"
echo "    Service: $SERVICE"
echo ""
echo "Monitor deployment:"
echo "    aws ecs describe-services --cluster $CLUSTER --services $SERVICE --region $ECR_REGION --query 'services[0].deployments' --no-cli-pager"
