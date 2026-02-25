#!/usr/bin/env bash
set -euo pipefail

if ! command -v openssl >/dev/null 2>&1; then
  echo "openssl is required but not found on PATH" >&2
  exit 1
fi

read -rsp "Syncfusion license key: " SYNCFUSION_LICENSE_KEY
echo
read -rsp "Passphrase (store this in 1Password): " SYNCFUSION_LICENSE_PASSPHRASE
echo

if [[ -z "${SYNCFUSION_LICENSE_KEY}" ]]; then
  echo "License key cannot be empty" >&2
  exit 1
fi

if [[ -z "${SYNCFUSION_LICENSE_PASSPHRASE}" ]]; then
  echo "Passphrase cannot be empty" >&2
  exit 1
fi

blob=$(printf "%s" "${SYNCFUSION_LICENSE_KEY}" \
  | openssl enc -aes-256-cbc -pbkdf2 -iter 210000 -salt -a -A -pass pass:"${SYNCFUSION_LICENSE_PASSPHRASE}")

echo
echo "Encrypted blob (commit this in SyncfusionLicenseSecret.EncryptedLicenseBlob):"
echo "${blob}"
echo
echo "Set this at runtime from 1Password:"
echo "export SYNCFUSION_LICENSE_PASSPHRASE='<your-passphrase>'"