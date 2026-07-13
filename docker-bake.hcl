// docker-bake.hcl
// Builds 3 services (multi-arch) from local Dockerfiles and pushes to GHCR.
// Image names must be LOWERCASE.

variable "REGISTRY_GHCR" { default = "ghcr.io" }
variable "OWNER"         { default = "hasanjaved-developer" }   // overridden by workflow
variable "REPO_SLUG"     { default = "errorlog-analyzer" }     // your namespace/group
variable "DOCKERHUB_NAMESPACE" { default = "hasanjaveddeveloper" }                  // optional

group "default" {
  targets = ["api", "web"]
}

/* ---------------------- api (ErrorAnalyzerApi) ---------------------- */
target "api" {
  context    = "."
  dockerfile = "./src/ErrorAnalyserApi/Dockerfile"

  tags = [
    "${REGISTRY_GHCR}/${OWNER}/${REPO_SLUG}/api:edge"
  ]

  platforms = ["linux/amd64", "linux/arm64"]
  labels = {
    "org.opencontainers.image.source" = "https://github.com/${OWNER}/ErrorLogAnalyzer"
    "org.opencontainers.image.title"  = "api"
  }
}

/* ---------------------- web (IntegrationPortal) ---------------------- */
target "web" {
  context    = "."
  dockerfile = "./src/ApiIntegrationMvc/Dockerfile"

  tags = [
    "${REGISTRY_GHCR}/${OWNER}/${REPO_SLUG}/web:edge"
  ]

  platforms = ["linux/amd64", "linux/arm64"]
  labels = {
    "org.opencontainers.image.source" = "https://github.com/${OWNER}/ErrorLogAnalyzer"
    "org.opencontainers.image.title"  = "web"
  }
}