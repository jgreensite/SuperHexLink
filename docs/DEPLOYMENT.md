# Deployment Guide

This document covers the deployment processes for SuperHexLink, including staging and production environments.

**Last updated**: February 2026

---

## Overview

SuperHexLink uses automated deployment pipelines with comprehensive testing, security scanning, and rollback capabilities. The deployment process is designed to be safe, observable, and reversible.

### Deployment Environments

| Environment | Purpose | URL | Access |
|-------------|---------|-----|--------|
| **Staging** | Pre-production testing | https://staging.superhexlink.com | Internal team |
| **Production** | Live user environment | https://superhexlink.com | Public |

---

## Staging Deployment

### Automated Staging Pipeline

The staging deployment is triggered automatically on pushes to `main` and `develop` branches.

**Trigger Conditions**:
- Push to `main` or `develop` branches
- Changes to Unity assets, project settings, packages, or deployment workflow
- Manual dispatch via GitHub Actions

**Pipeline Stages**:

1. **Build and Test**
   - Unity build for StandaloneWindows64
   - All unit tests and integration tests
   - Build artifact upload (7-day retention)

2. **Security Scan**
   - Trivy vulnerability scanner
   - SARIF format results uploaded to GitHub Security

3. **Deploy to Staging**
   - Download build artifacts
   - Create staging configuration
   - Deploy to staging servers
   - Health checks and smoke tests

4. **Cleanup**
   - Remove temporary resources
   - Generate rollback information

### Staging Configuration

Staging uses environment-specific configuration:

```json
{
  "serverHost": "staging.superhexlink.com",
  "serverPort": 6321,
  "connectionTimeoutMs": 5000,
  "maxRetries": 3,
  "enableLogging": true,
  "_comment": "Staging environment configuration"
}
```

### Staging Smoke Tests

Comprehensive smoke tests validate the staging deployment:

- **Configuration Validation**: Config files exist and contain correct values
- **Build Artifacts**: Executable and supporting files present
- **Functionality Tests**: CoreLogic, save/load, configuration loading
- **Environment Tests**: Environment variables work correctly
- **Unity Settings**: Staging-specific player settings and scripting defines

### Manual Staging Deployment

For manual staging deployments:

```bash
# Trigger staging deployment manually
gh workflow run deploy-staging.yml \
  --ref main \
  --field force_deploy=false
```

---

## Production Deployment

### Production Deployment Pipeline

Production deployment follows a more rigorous process with manual approval gates.

**Trigger Conditions**:
- Manual trigger after successful staging validation
- All tests passing in staging
- Security scan results acceptable
- Manual approval from production gatekeepers

**Pipeline Stages**:

1. **Pre-deployment Checks**
   - Staging environment health verification
   - Performance benchmarks validation
   - Security scan review

2. **Build Production Artifact**
   - Unity build with production optimizations
   - Production configuration generation
   - Build manifest creation

3. **Blue-Green Deployment**
   - Deploy to production environment
   - Health checks on new version
   - Traffic switching verification

4. **Post-deployment Validation**
   - Smoke tests in production
   - Performance monitoring
   - User experience validation

5. **Rollback Preparation**
   - Generate rollback information
   - Monitor for issues
   - Automatic rollback triggers

### Production Configuration

Production uses hardened configuration:

```json
{
  "serverHost": "prod.superhexlink.com",
  "serverPort": 6321,
  "connectionTimeoutMs": 3000,
  "maxRetries": 5,
  "enableLogging": false,
  "_comment": "Production environment configuration"
}
```

---

## Rollback Procedures

### Automatic Rollback

Rollbacks are triggered automatically when:

- Health checks fail
- Smoke tests fail
- Performance thresholds exceeded
- Error rates spike

### Manual Rollback

For manual rollbacks:

```bash
# Get rollback information from latest deployment
gh run download --name rollback-info

# Execute rollback command from rollback-plan.json
git checkout <previous-commit-hash>
./deploy-staging.sh  # or ./deploy-production.sh
```

### Rollback Validation

After rollback:

1. Verify previous version is running
2. Run smoke tests
3. Monitor system health
4. Notify stakeholders

---

## Environment Variables

### Staging Environment Variables

| Variable | Purpose | Default |
|----------|---------|---------|
| `SUPERHEX_SERVER_HOST` | Staging server hostname | `staging.superhexlink.com` |
| `SUPERHEX_SERVER_PORT` | Staging server port | `6321` |
| `SUPERHEX_CONNECTION_TIMEOUT` | Connection timeout | `5000` |
| `SUPERHEX_MAX_RETRIES` | Maximum retry attempts | `3` |
| `SUPERHEX_ENABLE_LOGGING` | Enable debug logging | `true` |

### Production Environment Variables

| Variable | Purpose | Default |
|----------|---------|---------|
| `SUPERHEX_SERVER_HOST` | Production server hostname | `prod.superhexlink.com` |
| `SUPERHEX_SERVER_PORT` | Production server port | `6321` |
| `SUPERHEX_CONNECTION_TIMEOUT` | Connection timeout | `3000` |
| `SUPERHEX_MAX_RETRIES` | Maximum retry attempts | `5` |
| `SUPERHEX_ENABLE_LOGGING` | Enable debug logging | `false` |

---

## Build Configuration

### Unity Build Settings

**Staging Build**:
- Target: StandaloneWindows64
- Development build: Enabled
- Script debugging: Enabled
- Compression: LZ4
- Scripting defines: `STAGING;UNITY_ASSERTIONS`

**Production Build**:
- Target: StandaloneWindows64
- Development build: Disabled
- Script debugging: Disabled
- Compression: LZ4HC
- Scripting defines: `PRODUCTION`

### Build Artifacts

Each deployment generates:

- **Executable**: Main application binary
- **Configuration**: Environment-specific config files
- **Manifest**: Build metadata and version information
- **Dependencies**: Required libraries and assets
- **Documentation**: README and LICENSE files

---

## Monitoring and Observability

### Health Checks

Health checks validate:

- Application startup
- Configuration loading
- Database connectivity
- External service availability

### Metrics

Key metrics monitored:

- Build success rate
- Deployment duration
- Error rates
- Performance benchmarks
- User experience metrics

### Logging

Deployment logs include:

- Build process details
- Test results
- Security scan findings
- Deployment steps
- Rollback information

---

## Troubleshooting

### Common Issues

**Build Failures**:
- Check Unity license configuration
- Verify project dependencies
- Review build logs for specific errors

**Test Failures**:
- Examine test output logs
- Check environment configuration
- Verify test data and fixtures

**Deployment Failures**:
- Review deployment logs
- Check target server connectivity
- Verify configuration files

**Rollback Issues**:
- Ensure previous commit is available
- Check rollback script permissions
- Verify environment state

### Debug Commands

```bash
# Check deployment status
gh run list --workflow deploy-staging.yml

# View deployment logs
gh run view <run-id> --log

# Download build artifacts
gh run download <run-id> --name staging-build

# Check environment variables
ServerConfig.LogEnvironmentVariables()
```

---

## Security Considerations

### Deployment Security

- **Secrets Management**: All secrets stored in GitHub Secrets
- **Access Control**: Role-based access to deployment pipelines
- **Audit Trail**: Complete audit log of all deployments
- **Vulnerability Scanning**: Automated security scans on all builds

### Environment Security

- **Network Isolation**: Staging and production environments isolated
- **Access Controls**: Restricted access to production servers
- **Data Protection**: Sensitive data encrypted at rest and in transit
- **Monitoring**: Security event monitoring and alerting

---

## Best Practices

### Deployment Best Practices

1. **Test in Staging First**: Always validate in staging before production
2. **Monitor Deployments**: Watch deployment progress and system health
3. **Prepare Rollbacks**: Always have rollback information ready
4. **Document Changes**: Maintain clear deployment documentation
5. **Communicate**: Notify stakeholders of deployment activities

### Configuration Management

1. **Environment-Specific Configs**: Use separate configs for each environment
2. **Version Control**: Track configuration changes in version control
3. **Validation**: Validate all configuration values
4. **Security**: Never commit secrets to version control
5. **Documentation**: Document all configuration options

---

## Related Documentation

- [Development Setup](DEV_SETUP.md) - Local development environment
- [Security Guidelines](SECURITY.md) - Security policies and procedures
- [Architecture](ARCHITECTURE.md) - System architecture and design
- [Testing Guide](TESTING.md) - Testing strategies and procedures
