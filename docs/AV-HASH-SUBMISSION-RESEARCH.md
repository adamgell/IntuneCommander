# Automating EXE Hash Submission to Antivirus Platforms

**Research Document**  
**Date:** February 2026  
**Objective:** Investigate and document approaches to automate the submission of executable (EXE) file hashes to Windows Defender and other major antivirus (AV) platforms.

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Microsoft Defender for Endpoint](#microsoft-defender-for-endpoint)
3. [Windows Defender Security Intelligence](#windows-defender-security-intelligence)
4. [VirusTotal](#virustotal)
5. [Hybrid Analysis (Falcon Sandbox)](#hybrid-analysis-falcon-sandbox)
6. [ANY.RUN](#anyrun)
7. [MetaDefender (OPSWAT)](#metadefender-opswat)
8. [AlienVault OTX](#alienvault-otx)
9. [Best Practices](#best-practices)
10. [How Organizations Implement Hash Submission Automation](#how-organizations-implement-hash-submission-automation)
11. [Summary Comparison Table](#summary-comparison-table)
12. [Example Scripts](#example-scripts)
13. [References](#references)

---

## Executive Summary

This document provides a comprehensive overview of automated hash submission to antivirus platforms, focusing on:

- **Microsoft Defender for Endpoint** — Enterprise-grade threat intelligence with hash lookup and submission capabilities via REST API
- **VirusTotal** — Industry-standard multi-engine scanning with hash lookup and file submission API
- **Hybrid Analysis** — Falcon Sandbox-powered automated malware analysis with hash queries and file uploads
- **ANY.RUN** — Interactive malware analysis sandbox with API support for hash queries and file submissions
- **MetaDefender (OPSWAT)** — Multi-engine scanning platform with hash lookup and file analysis
- **AlienVault OTX** — Open threat intelligence platform for sharing and querying IOCs including file hashes

All platforms support programmatic integration via REST APIs with varying levels of authentication, rate limiting, and feature availability. Scripts and workflow examples are provided in the [Example Scripts](#example-scripts) section.

---

## Microsoft Defender for Endpoint

### Overview

Microsoft Defender for Endpoint provides enterprise-grade threat intelligence and endpoint protection capabilities with robust API support for automating security operations.

### Capabilities

- **Hash Lookup**: Query file reputation by SHA-1, SHA-256, or MD5 hash
- **File Metadata**: Retrieve prevalence, classification (malicious/benign/suspicious), first/last seen dates
- **Submission for Review**: Via Microsoft 365 Defender portal (manual or semi-automated)
- **IOC Management**: Add custom file hash indicators for blocking or alerting

### API Endpoints

**Get File Information by Hash:**
```
GET https://api.security.microsoft.com/api/files/{hash}
```

**Query Parameters:**
- `{hash}`: SHA-1, SHA-256, or MD5 file hash

### Authentication

1. **Register App in Microsoft Entra ID (Azure AD)**
   - Navigate to Azure Portal → Entra ID → App Registrations → New Registration
   - Note Tenant ID and Client ID

2. **Configure API Permissions**
   - Add permission: `WindowsDefenderATP` → `File.Read.All`
   - Or Microsoft Graph: `SecurityAlert.Read.All`, `SecurityIncident.Read.All`
   - Grant admin consent

3. **Create Client Secret**
   - Under Certificates & secrets, create a new client secret

4. **Acquire OAuth 2.0 Token**
   ```
   POST https://login.microsoftonline.com/{tenant_id}/oauth2/v2.0/token
   
   Body:
   client_id={app_id}
   scope=https://api.securitycenter.microsoft.com/.default
   client_secret={app_secret}
   grant_type=client_credentials
   ```

### Limitations

- **Hash Lookup**: Available via API
- **Direct Hash Submission for Analysis**: Requires portal submission (not exposed via public API)
- **Rate Limits**: Standard Azure AD throttling applies
- **Permissions**: Requires enterprise Defender for Endpoint licensing

### Use Cases

- Validate file hash verdicts during incident response
- Automate threat intelligence enrichment in SOAR/SIEM
- Check hash reputation before deployment or execution

### Code Example

See [Example Scripts](#example-scripts) section for PowerShell and Python examples.

---

## Windows Defender Security Intelligence

### Overview

Windows Defender Security Intelligence is the consumer-facing component of Microsoft's antivirus ecosystem, integrated with Defender for Endpoint for enterprise scenarios.

### Capabilities

- **Hash Lookup**: Same API as Defender for Endpoint (`/api/files/{hash}`)
- **Portal Submission**: Manual file/hash submission at https://security.microsoft.com
- **Threat Intelligence**: Integrated with Microsoft's global threat intelligence network

### Submission Process

**Via Portal:**
1. Navigate to https://security.microsoft.com
2. Go to: Investigation & response → Actions & submissions → Submissions
3. Select "New submission" → Choose file or hash
4. Submit SHA-1 or SHA-256 hash for analysis

**Via API:**
- Same authentication and endpoints as Defender for Endpoint
- Direct hash submission for review requires portal access (no public API endpoint)

### Limitations

- Full submission capabilities require Defender for Endpoint Plan 2
- Consumer Windows Defender users have limited portal access
- API access requires Azure AD app registration

---

## VirusTotal

### Overview

VirusTotal is the industry-standard multi-engine antivirus scanning service, aggregating results from 70+ AV engines. It provides robust API support for hash queries and file submissions.

### Capabilities

- **Hash Lookup**: Query existing scan reports by MD5, SHA-1, or SHA-256
- **File Submission**: Upload files for multi-engine scanning
- **URL Scanning**: Submit URLs for analysis
- **Rescan**: Request rescanning of existing files with updated AV signatures

### API Endpoints

**Hash Lookup (API v3):**
```
GET https://www.virustotal.com/api/v3/files/{file_hash}
```

**File Upload:**
```
POST https://www.virustotal.com/api/v3/files
```

### Authentication

**API Key:**
- Sign up at https://www.virustotal.com
- Access API key from account settings
- Include in HTTP header: `x-apikey: YOUR_API_KEY`

### Limitations

- **Rate Limits**: 
  - Public API: 4 requests per minute
  - Premium API: Higher limits (contact VirusTotal for pricing)
- **File Size**: Max 650 MB for premium, 32 MB for free
- **Public Visibility**: Free submissions are public; premium offers private scanning

### Use Cases

- Quick multi-engine hash reputation checks
- Automated malware triage in security workflows
- Threat intelligence enrichment
- Integration with SOAR platforms (Splunk SOAR, Cortex XSOAR, etc.)

### Code Example

See [Example Scripts](#example-scripts) section for Python examples.

---

## Hybrid Analysis (Falcon Sandbox)

### Overview

Hybrid Analysis is powered by CrowdStrike's Falcon Sandbox, providing automated malware analysis with behavioral sandboxing, detonation, and IOC extraction.

### Capabilities

- **Hash Lookup**: Query existing sandbox reports by SHA-256, MD5, or SHA-1
- **File Upload**: Submit files for automated sandbox analysis
- **Sandbox Reports**: Detailed behavioral analysis, screenshots, network traffic, dropped files
- **IOC Extraction**: Automatic extraction of indicators (IPs, domains, registry keys, etc.)

### API Endpoints

**Hash Overview:**
```
GET https://www.hybrid-analysis.com/api/v2/overview/{sha256}
```

**Hash Search:**
```
GET https://www.hybrid-analysis.com/api/v2/search/hash
```

**File Upload:**
```
POST https://www.hybrid-analysis.com/api/v2/submit/file
```

### Authentication

**API Key:**
- Register at https://www.hybrid-analysis.com
- Access API key from user profile
- Include headers:
  ```
  api-key: YOUR_API_KEY
  User-Agent: Falcon Sandbox
  ```

### Python Package

**Installation:**
```bash
pip install git+https://github.com/dark0pcodes/hybrid_analysis_api.git
```

**Usage:**
```python
from hybrid_analysis_api import HybridAnalysis

ha = HybridAnalysis('YOUR_API_KEY')
result = ha.search_hash("SHA256_HASH_HERE")
overview = ha.overview_sha256("SHA256_HASH_HERE")
```

### Limitations

- **Rate Limits**: Varies by account tier (free vs. enterprise)
- **Public Submissions**: Free submissions are visible to community
- **File Requirements**: Actual file needed for sandbox analysis (hash alone insufficient for new analysis)

### Use Cases

- Automated malware sandboxing in incident response
- Behavioral analysis of suspicious executables
- IOC extraction for SIEM/SOAR integration
- Threat hunting and research

---

## ANY.RUN

### Overview

ANY.RUN provides interactive malware analysis with a unique approach allowing analysts to interact with samples during execution. Supports API automation for bulk operations.

### Capabilities

- **Hash Lookup**: Query existing analysis reports via Threat Intelligence API
- **File Upload**: Submit files for automated or interactive sandbox analysis
- **Interactive Mode**: Manually interact with samples during execution
- **Real-time Monitoring**: Watch analysis progress in browser
- **IOC Extraction**: Automatic extraction of network IOCs, file modifications, registry changes

### API Endpoints

**File Submission:**
```
POST https://api.any.run/v1/analysis
```

**Hash Lookup (Threat Intelligence):**
```python
from anyrun.connectors import TiLookupConnector
with TiLookupConnector(api_key) as conn:
    report = conn.get_analysis_by_hash('sha256_hash')
```

### Authentication

**API Key:**
- Register at https://any.run
- Access API key from account dashboard
- Include in authorization header: `Authorization: API-Key YOUR_API_KEY`

### Python SDK

**Official SDK:**
```bash
pip install git+https://github.com/anyrun/anyrun-sdk.git
```

**Example:**
```python
from anyrun.connectors import SandboxConnector, TiLookupConnector

api_key = "YOUR_API_KEY"
sha256_hash = "your_file_sha256"

# Check if hash exists
with TiLookupConnector(api_key) as conn:
    existing = conn.get_analysis_by_hash(sha256_hash)
    if existing:
        print("Found existing report:", existing)
    else:
        # Upload file if not found
        with SandboxConnector(api_key) as connector:
            task_id = connector.run_file_analysis("/path/to/file.exe")
            print("Analysis started:", task_id)
```

### Limitations

- **Hash-only queries**: Requires existing analysis (cannot sandbox without file)
- **Rate Limits**: Varies by subscription tier
- **File Size**: Check current limits on ANY.RUN docs
- **Interactive Features**: Some features require Hunter plan or higher

### Use Cases

- Interactive malware analysis for complex samples
- Automated bulk sample processing
- Training and education (watch malware behavior)
- Integration with SOC workflows

---

## MetaDefender (OPSWAT)

### Overview

MetaDefender by OPSWAT provides multi-engine scanning with support for 30+ AV engines, deep file inspection, and vulnerability assessment.

### Capabilities

- **Hash Lookup**: Query existing scan results by MD5, SHA-1, or SHA-256
- **File Upload**: Submit files for multi-engine scanning
- **Deep File Inspection**: Analyze file structure, metadata, embedded content
- **Vulnerability Assessment**: Check software versions for known vulnerabilities

### API Endpoints

**Hash Lookup:**
```
GET https://api.metadefender.com/v4/hash/{hash}
```

**File Upload:**
```
POST https://api.metadefender.com/v4/file
```

**Retrieve Results:**
```
GET https://api.metadefender.com/v4/file/{data_id}
```

### Authentication

**API Key:**
- Register at https://metadefender.opswat.com
- Access API key from account dashboard
- Include header: `apikey: YOUR_API_KEY`

### Workflow

1. **Check Hash**: Query hash endpoint to see if file was previously scanned
2. **Upload if New**: If hash not found, upload file via POST
3. **Poll for Results**: Use returned `data_id` to poll for completion (`progress_percentage: 100`)
4. **Parse Results**: Extract per-engine verdicts and overall classification

### Limitations

- **Rate Limits**: Varies by account tier
- **File Size**: Check current limits in OPSWAT docs
- **API Quotas**: Free tier has daily/monthly submission limits

### Use Cases

- Multi-engine validation of file safety
- Software supply chain security
- File reputation checks in CI/CD pipelines
- Compliance and audit requirements

---

## AlienVault OTX

### Overview

AlienVault Open Threat Exchange (OTX) is a community-driven threat intelligence platform where security professionals share IOCs including file hashes, IPs, domains, and URLs.

### Capabilities

- **Hash Lookup**: Query community-shared threat intelligence by file hash
- **Pulse Creation**: Share new IOCs with the community in "Pulses"
- **IOC Types**: Supports MD5, SHA-1, SHA-256, domains, IPs, URLs, CVEs, and more
- **Threat Intelligence**: Access global threat data from OTX contributors

### API Endpoints

**Hash Lookup:**
```
GET https://otx.alienvault.com/api/v1/indicators/file/{hash}/general
```

**Create Pulse (Submit Hashes):**
```
POST https://otx.alienvault.com/api/v1/pulses/create
```

### Authentication

**API Key:**
- Register at https://otx.alienvault.com
- Access API key from user dashboard
- Include header: `X-OTX-API-KEY: YOUR_API_KEY`

### Example: Submit Hash via Pulse

```python
import requests

api_key = "YOUR_OTX_API_KEY"
headers = {
    "X-OTX-API-KEY": api_key,
    "Content-Type": "application/json"
}
data = {
    "name": "Suspicious EXE Hashes - 2026-02",
    "description": "Collection of suspicious executable hashes",
    "indicators": [
        {
            "indicator": "d41d8cd98f00b204e9800998ecf8427e",
            "type": "FileHash-MD5",
            "title": "Suspicious Malware Sample"
        },
        {
            "indicator": "da39a3ee5e6b4b0d3255bfef95601890afd80709",
            "type": "FileHash-SHA1",
            "title": "Another Suspicious Sample"
        }
    ],
    "tlp": "white"
}
response = requests.post(
    "https://otx.alienvault.com/api/v1/pulses/create",
    headers=headers,
    json=data
)
print(response.status_code, response.json())
```

### Limitations

- **Community Data**: Quality varies based on contributor
- **Rate Limits**: Check OTX documentation for current limits
- **Public Sharing**: Pulses are visible to community (various TLP options)

### Use Cases

- Threat intelligence enrichment and correlation
- Sharing IOCs with security community
- SIEM/SOAR integration for hash reputation checks
- Collaborative threat hunting

---

## Best Practices

### 1. Hash Validation and Data Hygiene

- **Validate Format**: Ensure hashes are properly formatted (MD5: 32 hex chars, SHA-1: 40, SHA-256: 64)
- **Normalize Input**: Convert to lowercase or uppercase consistently
- **Separate IOC Types**: Don't mix hashes with IPs, domains, or URLs in the same field
- **Remove Special Characters**: Strip whitespace, line breaks, or other artifacts

### 2. Secure API Key Management

- **Never Hardcode Keys**: Use environment variables, secret managers (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault)
- **Rotate Keys Regularly**: Implement key rotation policies
- **Restrict Permissions**: Use least-privilege principle for API keys
- **Audit Key Usage**: Monitor and log all API key usage

### 3. Authentication Best Practices

- **Use OAuth 2.0**: Prefer OAuth over API keys when available (e.g., Microsoft Defender)
- **App Registration**: Register dedicated service accounts/apps for automation
- **Admin Consent**: Obtain proper admin consent for enterprise APIs
- **Certificate Auth**: Consider certificate-based auth for high-security scenarios

### 4. Rate Limiting and Throttling

- **Respect Rate Limits**: Implement exponential backoff for API calls
- **Cache Results**: Store hash lookup results to avoid duplicate queries
- **Batch Operations**: Group hash queries when supported
- **Monitor Quotas**: Track API usage against quotas

### 5. SOAR/SIEM Integration

- **Standardize Workflows**: Document and automate hash submission playbooks
- **Escalation Paths**: Define clear escalation for unknown or malicious hashes
- **Result Processing**: Automate verdict parsing and action triggers
- **Audit Logging**: Log all submissions and responses for compliance

### 6. Sample Submission Security

- **Encrypt Samples**: Use password-protected ZIP archives (password: "infected")
- **HTTPS Only**: Never transmit samples or hashes over unencrypted channels
- **Verify Recipients**: Ensure you're submitting to legitimate vendor endpoints
- **Data Privacy**: Review vendor privacy policies regarding submitted data

### 7. File Submission Escalation

- **Hash First**: Always query hash before uploading full file
- **Check File Size**: Respect vendor file size limits
- **Private Scanning**: Use private/paid options for sensitive files
- **Metadata Removal**: Consider stripping sensitive metadata before submission

### 8. Handling Results

- **Parse Verdicts**: Standardize how you interpret "malicious", "suspicious", "clean", "unknown"
- **Multi-Engine Consensus**: For VirusTotal/MetaDefender, use threshold (e.g., 3+ engines flagging)
- **False Positive Handling**: Implement process for disputing false positives
- **Automated Actions**: Quarantine, block, or alert based on verdict

### 9. Compliance and Privacy

- **Data Residency**: Be aware of where data is processed (cloud regions)
- **Retention Policies**: Understand how long vendors store submitted data
- **Public vs Private**: Know when submissions become public knowledge
- **Regulatory Alignment**: Ensure automation aligns with GDPR, HIPAA, etc.

### 10. Performance and Operational Safety

- **Avoid Duplicate Submissions**: Check local cache or database before querying APIs
- **Asynchronous Processing**: Use async/await patterns for bulk operations
- **Error Handling**: Implement robust retry logic with circuit breakers
- **Monitoring**: Alert on API failures, quota exhaustion, or unusual patterns

---

## How Organizations Implement Hash Submission Automation

### Overview

This section describes real-world implementation patterns used by enterprise Security Operations Centers (SOCs) and security teams to automate file hash submission and threat intelligence workflows.

### Common Architecture Patterns

#### 1. **SOAR-Based Automation (Most Common)**

**Architecture:**
```
Alert/Event → SIEM/EDR → SOAR Platform → Hash Extraction → 
API Query (VT/Defender/etc.) → Enrichment → Automated Response
```

**Popular SOAR Platforms:**
- **Splunk SOAR** (formerly Phantom)
- **Palo Alto Cortex XSOAR**
- **IBM QRadar SOAR**
- **Microsoft Sentinel** (with Logic Apps)
- **Tines** (no-code automation)
- **TheHive** (open-source)

**Typical Workflow:**
1. EDR/SIEM detects suspicious file activity
2. SOAR platform extracts file hash from alert
3. Automated playbook queries VirusTotal/Defender APIs
4. Results enriched into incident record
5. Conditional logic triggers response:
   - **Malicious** → Quarantine endpoint, block hash, create ticket, notify team
   - **Suspicious** → Escalate to analyst, add to watchlist
   - **Clean** → Close alert, document in logs
   - **Unknown** → Submit to sandbox, request deeper analysis

**Benefits:**
- Visual playbook editors for non-developers
- Out-of-the-box integrations with major AV platforms
- Standardized response across organization
- Compliance audit trails automatically maintained

**Example Implementation:**
```
Wazuh FIM Alert → Extract SHA256 → Query VirusTotal API → 
Parse Results → If malicious: Isolate endpoint + Slack alert + 
Create Jira ticket → Document in case management
```

#### 2. **Custom Script-Based Automation**

**Architecture:**
```
Detection System → Custom Python/PowerShell Script → 
AV API Calls → Result Processing → Action/Notification
```

**Common Implementations:**
- **Python scripts** with `requests` library for API calls
- **PowerShell** with `Invoke-RestMethod` for Windows environments
- **Bash scripts** with `curl` for Linux/Unix environments
- **Scheduled jobs** (cron/Task Scheduler) for batch processing

**Use Cases:**
- Smaller organizations without SOAR platforms
- Specific workflows not covered by SOAR
- Batch processing of threat intelligence feeds
- Custom integration with proprietary systems

**Example Script Pattern:**
```python
# Typical custom automation pattern
def process_hash(file_hash):
    # 1. Check local cache first
    if hash_in_cache(file_hash):
        return get_cached_result(file_hash)
    
    # 2. Query multiple services
    vt_result = query_virustotal(file_hash)
    defender_result = query_defender(file_hash)
    otx_result = query_alienvault_otx(file_hash)
    
    # 3. Aggregate results with consensus logic
    verdict = calculate_consensus(vt_result, defender_result, otx_result)
    
    # 4. Take action based on verdict
    if verdict == "malicious":
        block_hash_on_firewall(file_hash)
        send_slack_alert(file_hash, verdict)
        create_incident_ticket(file_hash)
    
    # 5. Cache result
    cache_result(file_hash, verdict)
    return verdict
```

#### 3. **SIEM Native Integration**

**Architecture:**
```
SIEM (Splunk/Sentinel/QRadar) → Built-in App/Connector → 
AV Service → Results Enrichment → Dashboard/Alert
```

**Popular Integrations:**
- **Splunk VirusTotal App** - Add-on for automatic hash enrichment
- **Microsoft Sentinel VirusTotal Connector** - Data connector for TI
- **QRadar VirusTotal Plugin** - Reference set enrichment
- **Elastic Security** - Threat Intel integration module

**Benefits:**
- Seamless integration with existing SIEM infrastructure
- Automatic enrichment of all file hashes in logs
- Unified threat intelligence across all security data
- No separate platform needed

**Example Flow:**
```
Windows Event Log → Splunk Indexing → Extract File Hash → 
VirusTotal App Lookup → Enrich Event → Alert if Malicious
```

#### 4. **EDR Platform Native Features**

**Architecture:**
```
EDR Agent → Cloud Backend → Built-in Threat Intel → 
Automatic Hash Checking → Policy Enforcement
```

**EDR Platforms with Native Hash Checking:**
- **CrowdStrike Falcon** - Automatic hash reputation via Falcon Intelligence
- **Microsoft Defender for Endpoint** - Integrated file intelligence
- **Carbon Black** - Cloud-based hash reputation service
- **SentinelOne** - Native threat intelligence integration
- **Palo Alto Cortex XDR** - AutoFocus threat intelligence

**Benefits:**
- Zero configuration required
- Real-time protection at endpoint
- No API rate limits to manage
- Integrated with detection and response

#### 5. **CI/CD Pipeline Integration**

**Architecture:**
```
Build Process → Artifact Generation → Hash Calculation → 
Security Gate (VT Check) → Pass/Fail Decision → Deploy/Block
```

**Implementation Patterns:**

**GitHub Actions Example:**
```yaml
- name: Calculate File Hash
  run: echo "HASH=$(sha256sum artifact.exe | awk '{print $1}')" >> $GITHUB_ENV

- name: Check with VirusTotal
  env:
    VT_API_KEY: ${{ secrets.VT_API_KEY }}
  run: |
    python scripts/check_hash.py ${{ env.HASH }}
    if [ $? -eq 2 ]; then
      echo "Malicious hash detected!"
      exit 1
    fi
```

**Jenkins Pipeline Example:**
```groovy
stage('Security Scan') {
    steps {
        script {
            def hash = sh(script: "sha256sum artifact.exe | awk '{print \$1}'", returnStdout: true).trim()
            def verdict = sh(script: "python check_hash.py ${hash}", returnStatus: true)
            if (verdict == 2) {
                error("Malicious file detected!")
            }
        }
    }
}
```

**Benefits:**
- Shift-left security approach
- Prevent malicious dependencies from deployment
- Automated compliance checks
- Integration with existing DevOps workflows

---

### Real-World Implementation Examples

#### Example 1: Enterprise SOC with SOAR

**Organization:** Large financial services company (5000+ endpoints)

**Architecture:**
- **Detection:** Microsoft Defender for Endpoint + Wazuh
- **Orchestration:** Splunk SOAR
- **Threat Intel:** VirusTotal Enterprise, Microsoft Defender TI
- **Ticketing:** ServiceNow
- **Communication:** Microsoft Teams

**Workflow:**
1. Defender detects suspicious executable on endpoint
2. SOAR extracts file hash (SHA-256)
3. Parallel API queries to VirusTotal and Defender TI
4. If 5+ AV engines flag as malicious:
   - Isolate endpoint via Defender API
   - Block hash in EDR policy
   - Create ServiceNow ticket (P1 priority)
   - Post alert to Teams security channel
   - Add hash to threat intelligence feed
5. All actions logged for compliance audit

**Results:**
- Reduced mean time to response (MTTR) from 2 hours to 5 minutes
- 95% of incidents handled automatically
- Analysts focus on complex threats only

#### Example 2: Mid-Size Organization with Custom Scripts

**Organization:** Healthcare provider (500 endpoints)

**Architecture:**
- **Detection:** Sysmon + Windows Event Forwarding
- **Processing:** Custom Python scripts on log server
- **Threat Intel:** VirusTotal (free tier)
- **Response:** PowerShell remoting for containment
- **Communication:** Email + Slack

**Workflow:**
1. Sysmon logs file creation events
2. Python script monitors event log
3. Extracts hashes from new executables
4. Queries VirusTotal (respecting 4/min rate limit)
5. Stores results in SQLite database
6. If malicious:
   - Email alert to security team
   - PowerShell remoting deletes file
   - Slack notification with details
7. Daily summary report generated

**Challenges Solved:**
- Free tier rate limiting handled with queue system
- Local caching reduces duplicate API calls
- Batch processing during off-hours for efficiency

#### Example 3: DevSecOps Pipeline

**Organization:** SaaS software company

**Architecture:**
- **CI/CD:** GitHub Actions
- **Artifact Scanning:** VirusTotal API
- **Dependency Checking:** GitHub Dependabot + Snyk
- **Container Scanning:** Trivy
- **Policy Enforcement:** OPA (Open Policy Agent)

**Workflow:**
1. Developer commits code
2. Build pipeline compiles binaries
3. SHA-256 hash calculated for all artifacts
4. Automated check against VirusTotal
5. Dependencies scanned for known vulnerabilities
6. Container image scanned with Trivy
7. Policy gate checks:
   - No malicious hashes allowed
   - No critical vulnerabilities
   - All dependencies up-to-date
8. Pass → Deploy to staging
9. Fail → Block deployment, notify developer

**Benefits:**
- Proactive security before production
- Automated supply chain security
- Developer-friendly security gates
- Complete audit trail for compliance

---

### Common Integration Tools and Libraries

#### Python Libraries
- **`requests`** - HTTP API calls to all platforms
- **`virustotal-python`** - Official VirusTotal SDK
- **`pydefenderatp`** - Defender for Endpoint wrapper
- **`OTXv2`** - AlienVault OTX Python SDK

#### PowerShell Modules
- **`Invoke-RestMethod`** - Built-in cmdlet for API calls
- **`DefenderAPI`** - Community Defender module
- **POSH-VirusTotal** - VirusTotal PowerShell wrapper

#### SOAR Connectors (Pre-built)
- **VirusTotal** - Available in Splunk SOAR, XSOAR, Tines, TheHive
- **Microsoft Defender** - Native in Sentinel, available in others
- **Hybrid Analysis** - Splunk SOAR, XSOAR integrations
- **AlienVault OTX** - QRadar SOAR, Graylog connectors

---

### Best Practices from Real Implementations

#### 1. **Implement Tiered Response Logic**
```
Unknown Hash → Check VT → If not found, submit to sandbox → 
If malicious, escalate to analyst → If clean, whitelist
```

#### 2. **Use Local Caching**
- Cache results for 24-48 hours to reduce API calls
- Store in Redis, SQLite, or PostgreSQL
- Key by hash, value includes verdict + timestamp
- Reduces costs and respects rate limits

#### 3. **Implement Circuit Breakers**
- If API fails 3+ times, pause automation for 5 minutes
- Prevents cascading failures
- Alerts team when API unavailable
- Queues hashes for later processing

#### 4. **Multi-Source Consensus**
```
If (VT: malicious) AND (Defender: malicious) → High confidence
If (VT: malicious) AND (Defender: clean) → Investigate
If (VT: unknown) → Submit to sandbox
```

#### 5. **Gradual Rollout**
- Start with read-only mode (query but don't act)
- Monitor false positive rate for 1-2 weeks
- Enable automated blocking for high-confidence cases only
- Expand automation scope gradually

#### 6. **Maintain Whitelist/Blacklist**
- Known good hashes (signed by organization)
- Known bad hashes (confirmed malware)
- Skip API calls for listed hashes
- Regularly review and update lists

#### 7. **Monitor and Measure**
- Track API success rate and latency
- Measure false positive/negative rates
- Monitor automation effectiveness (% auto-resolved)
- Review and tune thresholds monthly

---

### Cost Considerations

#### Free Tier Usage
- **VirusTotal:** 4 requests/minute = ~5,760 hashes/day
- **AlienVault OTX:** Free for community use
- **Strategy:** Suitable for small organizations or specific workflows

#### Paid Tier Benefits
- **VirusTotal Premium:** 
  - 1000+ requests/minute
  - Private scanning
  - Retrohunt and advanced queries
  - Cost: Contact for pricing (typically $$$-$$$$)
  
- **Defender for Endpoint:**
  - Included with E5 license
  - No per-API-call charges
  - Enterprise support
  
- **Hybrid Analysis Enterprise:**
  - Higher submission limits
  - Private sandboxing
  - Custom analysis profiles
  - Cost: Contact for pricing

#### Cost Optimization Strategies
1. **Use free tier for known hashes** (VT has huge database)
2. **Reserve paid tier for unknown/suspicious files**
3. **Implement aggressive caching** (24-48 hour TTL)
4. **Batch process non-urgent hashes** during off-peak
5. **Use local/on-prem sandboxes** for sensitive files

---

## Summary Comparison Table

| Platform | Hash Lookup | File Submission | Multi-Engine | Sandboxing | API Availability | Rate Limits | Cost |
|----------|-------------|-----------------|--------------|------------|------------------|-------------|------|
| **Microsoft Defender for Endpoint** | ✅ | ⚠️ Portal | ❌ | ✅ Limited | ✅ REST API | Standard Azure | Enterprise License |
| **VirusTotal** | ✅ | ✅ | ✅ 70+ engines | ❌ | ✅ REST API v3 | 4/min (free), higher (premium) | Free / Premium |
| **Hybrid Analysis** | ✅ | ✅ | ✅ Multiple | ✅ Full sandbox | ✅ REST API | Varies by tier | Free / Enterprise |
| **ANY.RUN** | ✅ | ✅ | ❌ | ✅ Interactive | ✅ REST API + SDK | Varies by tier | Free / Paid tiers |
| **MetaDefender** | ✅ | ✅ | ✅ 30+ engines | ❌ | ✅ REST API v4 | Varies by tier | Free / Paid tiers |
| **AlienVault OTX** | ✅ | ⚠️ Via Pulse | ❌ | ❌ | ✅ REST API | Check docs | Free |

**Legend:**
- ✅ = Fully supported
- ⚠️ = Partially supported or requires alternative method
- ❌ = Not supported

---

## Example Scripts

### PowerShell: Microsoft Defender for Endpoint Hash Lookup

```powershell
<#
.SYNOPSIS
    Query Microsoft Defender for Endpoint for file hash information
.DESCRIPTION
    Authenticates via Azure AD and queries Defender API for file hash reputation
.PARAMETER TenantId
    Azure AD Tenant ID
.PARAMETER AppId
    Azure AD App Registration Client ID
.PARAMETER AppSecret
    Azure AD App Registration Client Secret
.PARAMETER FileHash
    File hash to query (SHA-1 or SHA-256)
.EXAMPLE
    .\Get-DefenderHashInfo.ps1 -TenantId "tenant-guid" -AppId "app-guid" -AppSecret "secret" -FileHash "abc123..."
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$TenantId,
    
    [Parameter(Mandatory=$true)]
    [string]$AppId,
    
    [Parameter(Mandatory=$true)]
    [string]$AppSecret,
    
    [Parameter(Mandatory=$true)]
    [string]$FileHash
)

# Acquire OAuth token
$tokenUri = "https://login.microsoftonline.com/$TenantId/oauth2/v2.0/token"
$body = @{
    client_id     = $AppId
    scope         = "https://api.securitycenter.microsoft.com/.default"
    client_secret = $AppSecret
    grant_type    = "client_credentials"
}

Write-Host "Acquiring access token..." -ForegroundColor Cyan
$tokenResponse = Invoke-RestMethod -Method Post -Uri $tokenUri -Body $body
$token = $tokenResponse.access_token

# Query file hash
$uri = "https://api.security.microsoft.com/api/files/$FileHash"
$headers = @{
    Authorization = "Bearer $token"
}

Write-Host "Querying hash: $FileHash" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Headers $headers -Uri $uri -Method Get
    
    Write-Host "`nFile Information:" -ForegroundColor Green
    Write-Host "  SHA1: $($response.sha1)"
    Write-Host "  SHA256: $($response.sha256)"
    Write-Host "  Classification: $($response.globalPrevalence)"
    Write-Host "  First Seen: $($response.globalFirstObserved)"
    Write-Host "  Last Seen: $($response.globalLastObserved)"
    
    return $response
}
catch {
    Write-Host "Error querying hash: $_" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}
```

### Python: VirusTotal Hash Lookup

```python
#!/usr/bin/env python3
"""
VirusTotal Hash Lookup Script

Query VirusTotal API v3 for file hash reputation and scan results.

Usage:
    python virustotal_hash_lookup.py <file_hash>

Environment Variables:
    VT_API_KEY: VirusTotal API key
"""

import sys
import os
import requests
import json
from typing import Dict, Optional

def get_virustotal_report(file_hash: str, api_key: str) -> Optional[Dict]:
    """
    Query VirusTotal for file hash report.
    
    Args:
        file_hash: MD5, SHA-1, or SHA-256 file hash
        api_key: VirusTotal API key
        
    Returns:
        Dict containing scan results or None on error
    """
    url = f"https://www.virustotal.com/api/v3/files/{file_hash}"
    headers = {
        "x-apikey": api_key
    }
    
    try:
        response = requests.get(url, headers=headers)
        response.raise_for_status()
        return response.json()
    except requests.exceptions.HTTPError as e:
        if e.response.status_code == 404:
            print(f"Hash not found in VirusTotal: {file_hash}")
        else:
            print(f"HTTP Error: {e}")
        return None
    except requests.exceptions.RequestException as e:
        print(f"Error querying VirusTotal: {e}")
        return None

def parse_scan_results(data: Dict) -> None:
    """
    Parse and display VirusTotal scan results.
    
    Args:
        data: VirusTotal API response data
    """
    if not data or "data" not in data:
        print("No scan data available")
        return
    
    attributes = data["data"]["attributes"]
    stats = attributes.get("last_analysis_stats", {})
    
    print("\n" + "="*60)
    print("VirusTotal Scan Results")
    print("="*60)
    
    # File hashes
    print(f"\nMD5:    {attributes.get('md5', 'N/A')}")
    print(f"SHA1:   {attributes.get('sha1', 'N/A')}")
    print(f"SHA256: {attributes.get('sha256', 'N/A')}")
    
    # Detection statistics
    print(f"\nDetection Summary:")
    print(f"  Malicious:     {stats.get('malicious', 0)}")
    print(f"  Suspicious:    {stats.get('suspicious', 0)}")
    print(f"  Undetected:    {stats.get('undetected', 0)}")
    print(f"  Harmless:      {stats.get('harmless', 0)}")
    print(f"  Timeout:       {stats.get('timeout', 0)}")
    print(f"  Unsupported:   {stats.get('type-unsupported', 0)}")
    
    # Overall verdict
    malicious_count = stats.get('malicious', 0)
    suspicious_count = stats.get('suspicious', 0)
    total_engines = sum(stats.values())
    
    print(f"\nOverall: {malicious_count + suspicious_count}/{total_engines} engines flagged this file")
    
    if malicious_count > 0:
        print("Verdict: ⚠️  MALICIOUS")
    elif suspicious_count > 0:
        print("Verdict: ⚠️  SUSPICIOUS")
    else:
        print("Verdict: ✅ CLEAN")
    
    # File metadata
    print(f"\nFile Size: {attributes.get('size', 'N/A')} bytes")
    print(f"File Type: {attributes.get('type_description', 'N/A')}")
    print(f"First Submission: {attributes.get('first_submission_date', 'N/A')}")
    print(f"Last Analysis: {attributes.get('last_analysis_date', 'N/A')}")
    
    # Detailed engine results
    print("\nDetailed Engine Results:")
    results = attributes.get("last_analysis_results", {})
    
    # Show only engines that detected as malicious or suspicious
    flagged = {
        engine: result 
        for engine, result in results.items() 
        if result.get("category") in ["malicious", "suspicious"]
    }
    
    if flagged:
        for engine, result in sorted(flagged.items()):
            category = result.get("category", "unknown")
            result_text = result.get("result", "N/A")
            print(f"  [{category.upper()}] {engine}: {result_text}")
    else:
        print("  No engines flagged this file")
    
    print("="*60 + "\n")

def main():
    """Main execution function."""
    if len(sys.argv) < 2:
        print("Usage: python virustotal_hash_lookup.py <file_hash>")
        print("\nEnvironment Variables:")
        print("  VT_API_KEY: VirusTotal API key")
        sys.exit(1)
    
    file_hash = sys.argv[1]
    api_key = os.environ.get("VT_API_KEY")
    
    if not api_key:
        print("Error: VT_API_KEY environment variable not set")
        print("Set it with: export VT_API_KEY='your-api-key'")
        sys.exit(1)
    
    print(f"Querying VirusTotal for hash: {file_hash}")
    data = get_virustotal_report(file_hash, api_key)
    
    if data:
        parse_scan_results(data)
    else:
        sys.exit(1)

if __name__ == "__main__":
    main()
```

### Python: Multi-Platform Hash Checker

```python
#!/usr/bin/env python3
"""
Multi-Platform Hash Checker

Query multiple AV platforms for file hash reputation.

Supported Platforms:
    - VirusTotal
    - Hybrid Analysis
    - AlienVault OTX
    - MetaDefender

Usage:
    python multi_platform_checker.py <file_hash>

Environment Variables:
    VT_API_KEY: VirusTotal API key
    HA_API_KEY: Hybrid Analysis API key
    OTX_API_KEY: AlienVault OTX API key
    MD_API_KEY: MetaDefender API key
"""

import sys
import os
import requests
from dataclasses import dataclass
from typing import Dict, Optional, List
from enum import Enum

class Verdict(Enum):
    """Hash verdict enumeration."""
    MALICIOUS = "malicious"
    SUSPICIOUS = "suspicious"
    CLEAN = "clean"
    UNKNOWN = "unknown"

@dataclass
class HashResult:
    """Hash check result from a platform."""
    platform: str
    verdict: Verdict
    details: str
    error: Optional[str] = None

def check_virustotal(file_hash: str, api_key: str) -> HashResult:
    """Check hash on VirusTotal."""
    if not api_key:
        return HashResult("VirusTotal", Verdict.UNKNOWN, "API key not provided")
    
    url = f"https://www.virustotal.com/api/v3/files/{file_hash}"
    headers = {"x-apikey": api_key}
    
    try:
        response = requests.get(url, headers=headers, timeout=10)
        if response.status_code == 404:
            return HashResult("VirusTotal", Verdict.UNKNOWN, "Hash not found")
        
        response.raise_for_status()
        data = response.json()
        
        stats = data["data"]["attributes"]["last_analysis_stats"]
        malicious = stats.get("malicious", 0)
        suspicious = stats.get("suspicious", 0)
        total = sum(stats.values())
        
        details = f"{malicious + suspicious}/{total} engines flagged"
        
        if malicious > 3:
            verdict = Verdict.MALICIOUS
        elif malicious > 0 or suspicious > 0:
            verdict = Verdict.SUSPICIOUS
        else:
            verdict = Verdict.CLEAN
        
        return HashResult("VirusTotal", verdict, details)
    
    except Exception as e:
        return HashResult("VirusTotal", Verdict.UNKNOWN, "Error", str(e))

def check_hybrid_analysis(file_hash: str, api_key: str) -> HashResult:
    """Check hash on Hybrid Analysis."""
    if not api_key:
        return HashResult("Hybrid Analysis", Verdict.UNKNOWN, "API key not provided")
    
    # Hybrid Analysis requires SHA256
    if len(file_hash) != 64:
        return HashResult("Hybrid Analysis", Verdict.UNKNOWN, "SHA-256 required")
    
    url = f"https://www.hybrid-analysis.com/api/v2/overview/{file_hash}"
    headers = {
        "api-key": api_key,
        "User-Agent": "Falcon Sandbox"
    }
    
    try:
        response = requests.get(url, headers=headers, timeout=10)
        if response.status_code == 404:
            return HashResult("Hybrid Analysis", Verdict.UNKNOWN, "Hash not found")
        
        response.raise_for_status()
        data = response.json()
        
        threat_score = data.get("threat_score", 0)
        verdict_str = data.get("verdict", "unknown").lower()
        
        if "malicious" in verdict_str or threat_score >= 70:
            verdict = Verdict.MALICIOUS
        elif "suspicious" in verdict_str or threat_score >= 30:
            verdict = Verdict.SUSPICIOUS
        elif threat_score == 0:
            verdict = Verdict.CLEAN
        else:
            verdict = Verdict.UNKNOWN
        
        details = f"Threat score: {threat_score}, Verdict: {verdict_str}"
        return HashResult("Hybrid Analysis", verdict, details)
    
    except Exception as e:
        return HashResult("Hybrid Analysis", Verdict.UNKNOWN, "Error", str(e))

def check_otx(file_hash: str, api_key: str) -> HashResult:
    """Check hash on AlienVault OTX."""
    if not api_key:
        return HashResult("AlienVault OTX", Verdict.UNKNOWN, "API key not provided")
    
    url = f"https://otx.alienvault.com/api/v1/indicators/file/{file_hash}/general"
    headers = {"X-OTX-API-KEY": api_key}
    
    try:
        response = requests.get(url, headers=headers, timeout=10)
        if response.status_code == 404:
            return HashResult("AlienVault OTX", Verdict.UNKNOWN, "Hash not found")
        
        response.raise_for_status()
        data = response.json()
        
        pulse_count = data.get("pulse_info", {}).get("count", 0)
        
        if pulse_count > 0:
            verdict = Verdict.SUSPICIOUS
            details = f"Found in {pulse_count} threat pulses"
        else:
            verdict = Verdict.CLEAN
            details = "No threat intelligence found"
        
        return HashResult("AlienVault OTX", verdict, details)
    
    except Exception as e:
        return HashResult("AlienVault OTX", Verdict.UNKNOWN, "Error", str(e))

def check_metadefender(file_hash: str, api_key: str) -> HashResult:
    """Check hash on MetaDefender."""
    if not api_key:
        return HashResult("MetaDefender", Verdict.UNKNOWN, "API key not provided")
    
    url = f"https://api.metadefender.com/v4/hash/{file_hash}"
    headers = {"apikey": api_key}
    
    try:
        response = requests.get(url, headers=headers, timeout=10)
        if response.status_code == 404:
            return HashResult("MetaDefender", Verdict.UNKNOWN, "Hash not found")
        
        response.raise_for_status()
        data = response.json()
        
        scan_results = data.get("scan_results", {})
        total_detected = scan_results.get("total_detected_avs", 0)
        total_avs = scan_results.get("total_avs", 1)
        
        details = f"{total_detected}/{total_avs} engines detected threat"
        
        if total_detected > 3:
            verdict = Verdict.MALICIOUS
        elif total_detected > 0:
            verdict = Verdict.SUSPICIOUS
        else:
            verdict = Verdict.CLEAN
        
        return HashResult("MetaDefender", verdict, details)
    
    except Exception as e:
        return HashResult("MetaDefender", Verdict.UNKNOWN, "Error", str(e))

def print_results(results: List[HashResult]) -> None:
    """Print formatted results."""
    print("\n" + "="*70)
    print("Multi-Platform Hash Check Results")
    print("="*70)
    
    # Define verdict symbols
    symbols = {
        Verdict.MALICIOUS: "🔴",
        Verdict.SUSPICIOUS: "🟡",
        Verdict.CLEAN: "🟢",
        Verdict.UNKNOWN: "⚪"
    }
    
    for result in results:
        symbol = symbols.get(result.verdict, "⚪")
        print(f"\n{symbol} {result.platform}")
        print(f"  Verdict: {result.verdict.value.upper()}")
        print(f"  Details: {result.details}")
        if result.error:
            print(f"  Error: {result.error}")
    
    # Overall assessment
    print("\n" + "-"*70)
    malicious_count = sum(1 for r in results if r.verdict == Verdict.MALICIOUS)
    suspicious_count = sum(1 for r in results if r.verdict == Verdict.SUSPICIOUS)
    
    if malicious_count > 0:
        print("⚠️  OVERALL: MALICIOUS - Multiple platforms flagged this hash")
    elif suspicious_count > 1:
        print("⚠️  OVERALL: SUSPICIOUS - Multiple platforms flagged this hash")
    elif suspicious_count == 1:
        print("⚠️  OVERALL: SUSPICIOUS - One platform flagged this hash")
    else:
        print("✅ OVERALL: CLEAN - No threats detected")
    
    print("="*70 + "\n")

def main():
    """Main execution function."""
    if len(sys.argv) < 2:
        print("Usage: python multi_platform_checker.py <file_hash>")
        print("\nEnvironment Variables:")
        print("  VT_API_KEY: VirusTotal API key")
        print("  HA_API_KEY: Hybrid Analysis API key")
        print("  OTX_API_KEY: AlienVault OTX API key")
        print("  MD_API_KEY: MetaDefender API key")
        sys.exit(1)
    
    file_hash = sys.argv[1].lower()
    
    # Get API keys from environment
    vt_key = os.environ.get("VT_API_KEY")
    ha_key = os.environ.get("HA_API_KEY")
    otx_key = os.environ.get("OTX_API_KEY")
    md_key = os.environ.get("MD_API_KEY")
    
    print(f"Checking hash across multiple platforms: {file_hash}")
    print("This may take a few seconds...\n")
    
    # Check all platforms
    results = []
    results.append(check_virustotal(file_hash, vt_key))
    results.append(check_hybrid_analysis(file_hash, ha_key))
    results.append(check_otx(file_hash, otx_key))
    results.append(check_metadefender(file_hash, md_key))
    
    print_results(results)

if __name__ == "__main__":
    main()
```

---

## References

### Microsoft Defender for Endpoint
- [Access the Microsoft Defender for Endpoint APIs](https://learn.microsoft.com/en-us/defender-endpoint/api/apis-intro)
- [Get file information API](https://learn.microsoft.com/en-us/defender-endpoint/api/get-file-information)
- [Submit files in Microsoft Defender for Endpoint](https://learn.microsoft.com/en-us/defender-endpoint/admin-submissions-mde)
- [Use Microsoft Defender for Endpoint APIs](https://learn.microsoft.com/en-us/defender-endpoint/api/exposed-apis-create-app-nativeapp)
- [GitHub: Defender Endpoint Samples](https://github.com/jcoliz/defender-endpoint-samples)

### VirusTotal
- [VirusTotal API v3 Overview](https://docs.virustotal.com/reference/overview)
- [API Overview Documentation](https://docs.virustotal.com/docs/api-overview)
- [File Info API Reference](https://docs.virustotal.com/reference/file-info)
- [Python virustotal3 Library](https://virustotal3.readthedocs.io/en/latest/)

### Hybrid Analysis
- [Hybrid Analysis | Sumo Logic Docs](https://www.sumologic.com/help/docs/platform-services/automation-service/app-central/integrations/hybrid-analysis/)
- [GitHub: hybrid_analysis_api Python Package](https://github.com/dark0pcodes/hybrid_analysis_api)
- [Cyware Orchestrate Integration](https://techdocs.cyware.com/co/en/hybrid-analysis.html)

### ANY.RUN
- [ANY.RUN API Documentation](https://any.run/api-documentation/)
- [GitHub: Official Python SDK](https://github.com/anyrun/anyrun-sdk)
- [Automated Malware Analysis for SOCs](https://any.run/cybersecurity-blog/automated-interactivity/)

### MetaDefender (OPSWAT)
- [MetaDefender Cloud API v4](https://www.opswat.com/docs/mdcloud/metadefender-cloud-api-v4)
- [GitHub: OPSWAT MetaDefender Examples](https://github.com/surendrap720/OPSWAT-METADEFENDER)

### AlienVault OTX
- [LevelBlue External API Documentation](https://otx.alienvault.com/assets/static/external_api.html)
- [GitHub: AlienVault IOC Script](https://github.com/tergin-dev/alienvaultioc)
- [QRadar SOAR Apps - AlienVault OTX](https://ibmresilient.github.io/resilient-community-apps/fn_alienvault_otx/README.html)

### Best Practices
- [Security Workflow Automation Best Practices](https://www.tines.com/blog/security-automation/security-workflow-automation/)
- [Integrating Anti-Malware into CI/CD Pipelines](https://dev.to/kedster/integrating-anti-malware-into-cicd-pipelines-for-proactive-threat-detection-g0o)
- [Microsoft Graph Security API Authorization](https://learn.microsoft.com/en-us/graph/security-authorization)

---

## Conclusion

This research document provides comprehensive guidance for automating EXE hash submission to major antivirus platforms. Key takeaways:

1. **Microsoft Defender for Endpoint** offers enterprise-grade integration but requires portal access for full submission capabilities
2. **VirusTotal** remains the industry standard for multi-engine scanning with excellent API support
3. **Hybrid Analysis, ANY.RUN** provide advanced sandboxing capabilities for behavioral analysis
4. **MetaDefender, AlienVault OTX** offer additional threat intelligence and community-driven insights

All platforms support REST APIs with varying authentication methods, rate limits, and capabilities. The example scripts provided demonstrate practical implementation patterns for automated hash checking and submission workflows.

For production implementations, follow the best practices outlined in this document, paying special attention to API key security, rate limiting, and result interpretation.
