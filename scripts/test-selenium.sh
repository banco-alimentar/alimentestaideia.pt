#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
selection="${1:-}"
requires_mailpit=false
if [[ $# -gt 0 ]]; then
    shift
fi

case "$selection" in
    local)
        requires_mailpit=true
        export SeleniumTest__Target=Local
        export SeleniumTest__Local__SelfContained="${SeleniumTest__Local__SelfContained:-true}"
        test_filter='Category=Referral|Category=LocalEmail'
        case "$SeleniumTest__Local__SelfContained" in
            [Tt][Rr][Uu][Ee]) test_filter+='|Category=LocalBrowser' ;;
        esac
        ;;
    local-email)
        requires_mailpit=true
        export SeleniumTest__Target=Local
        export SeleniumTest__Local__SelfContained="${SeleniumTest__Local__SelfContained:-true}"
        test_filter='Category=LocalEmail'
        ;;
    developer)
        export SeleniumTest__Target=Developer
        test_filter='Category=Referral|Category=LivePayment'
        ;;
    offline)
        test_filter='Category=Offline'
        ;;
    *)
        echo 'Usage: bash scripts/test-selenium.sh {local|local-email|developer|offline} [dotnet test options]' >&2
        exit 2
        ;;
esac

# Listing tests does not start browser cases, so it does not need a running Mailpit instance.
list_only=false
for argument in "$@"; do
    if [[ "$argument" == "--list-tests" ]]; then
        list_only=true
        break
    fi
done

if [[ "$requires_mailpit" == true && "$list_only" == false ]]; then
    mailpit_base_url="${SeleniumTest__Local__MailpitBaseUrl:-http://127.0.0.1:8025}"
    mailpit_smtp_port="${SeleniumTest__Local__MailpitSmtpPort:-1025}"

    if [[ ! "$mailpit_base_url" =~ ^https?://(localhost|127\.0\.0\.1|\[::1\])(:[0-9]{1,5})?/?$ ]]; then
        echo 'Local Selenium requires Mailpit to use a loopback URL such as http://127.0.0.1:8025.' >&2
        exit 1
    fi

    if ! command -v curl >/dev/null 2>&1; then
        echo 'Cannot check Mailpit because curl is unavailable; install curl and retry.' >&2
        exit 1
    fi

    mailpit_api_url="${mailpit_base_url%/}/api/v1/messages"
    if ! curl --fail --silent --show-error --connect-timeout 2 --max-time 5 "$mailpit_api_url" >/dev/null; then
        echo "Mailpit is not responding at $mailpit_api_url; start Mailpit before running local Selenium tests." >&2
        exit 1
    fi

    if ! command -v nc >/dev/null 2>&1; then
        echo 'Cannot check Mailpit SMTP because nc is unavailable; install netcat and retry.' >&2
        exit 1
    fi

    if ! nc -z -w 2 127.0.0.1 "$mailpit_smtp_port" >/dev/null 2>&1; then
        echo "Mailpit SMTP is not responding on 127.0.0.1:$mailpit_smtp_port; start Mailpit before running local Selenium tests." >&2
        exit 1
    fi
fi

# Prevent a second filter from accidentally including live payment or email-only cases.
for argument in "$@"; do
    case "$argument" in
        --filter|--filter=*|--settings|--settings=*|-s|-s:*)
            echo 'This runner owns its test selection; use dotnet test directly for custom filters/settings.' >&2
            exit 2
            ;;
    esac
done

exec dotnet test "$repo_root/BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest/BancoAlimentar.AlimentaEstaIdeia.Web.Selenium.UITest.csproj" \
    --configuration Release --filter "$test_filter" "$@"
