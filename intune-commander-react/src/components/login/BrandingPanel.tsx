export function BrandingPanel() {
  return (
    <div className="branding-panel">
      <div className="branding-content">
        <div className="branding-logo-placeholder">IC</div>
        <h1 className="branding-title">Intune Commander</h1>
        <p className="branding-subtitle">
          The ultimate desktop client for Microsoft Intune.
        </p>
        <div className="branding-features">
          <div className="branding-feature">
            <span className="branding-feature-icon">&#9889;</span>
            <span className="branding-feature-text">Lightning fast native performance</span>
          </div>
          <div className="branding-feature">
            <span className="branding-feature-icon">&#128274;</span>
            <span className="branding-feature-text">Multi-cloud &amp; multi-tenant support</span>
          </div>
          <div className="branding-feature">
            <span className="branding-feature-icon">&#128196;</span>
            <span className="branding-feature-text">Bulk export and reporting</span>
          </div>
        </div>
      </div>
    </div>
  );
}
