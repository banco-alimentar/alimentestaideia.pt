#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
selection="${1:-}"
if [[ $# -gt 0 ]]; then
    shift
fi

case "$selection" in
    local)
        export SeleniumTest__Target=Local
        export SeleniumTest__Local__SelfContained="${SeleniumTest__Local__SelfContained:-true}"
        test_filter='Category=Referral|Category=LocalEmail'
        case "$SeleniumTest__Local__SelfContained" in
            [Tt][Rr][Uu][Ee]) test_filter+='|Category=LocalBrowser' ;;
        esac
        ;;
    local-email)
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
