#!/usr/bin/env python3
"""Validate explicit TRX reports without rewriting them or exposing test payloads."""

import sys
import xml.etree.ElementTree as ET


NAMESPACE = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"


def verify_report(path):
    """Return the passing case count or raise a sanitized validation error."""
    try:
        root = ET.parse(path).getroot()
    except (OSError, ET.ParseError, ValueError):
        raise ValueError("Missing, unreadable, or malformed TRX report.") from None

    if root.tag != NAMESPACE + "TestRun":
        raise ValueError("Expected a TRX TestRun document.")

    results = root.findall(f"{NAMESPACE}Results/{NAMESPACE}UnitTestResult")
    if not results:
        raise ValueError("The report contains no executed test results.")

    for attribute in ("testName", "testId", "executionId"):
        identities = [result.get(attribute, "").strip() for result in results]
        if any(not identity for identity in identities):
            raise ValueError("Test results have missing identities.")
        if len(set(identities)) != len(identities):
            # Azure may collapse equal display names even when TRX IDs differ.
            raise ValueError("Test results have duplicate names or identities.")

    if any(result.get("outcome") != "Passed" for result in results):
        raise ValueError("Every required test must pass; incomplete or skipped results are not accepted.")

    summary = root.find(NAMESPACE + "ResultSummary")
    if summary is None or summary.get("outcome") not in ("Completed", "Passed"):
        raise ValueError("The test run did not complete successfully.")
    counters = summary.find(NAMESPACE + "Counters")
    if counters is None:
        raise ValueError("The report has no result counters.")

    expected = {"total": len(results), "executed": len(results), "passed": len(results), "failed": 0, "notExecuted": 0}
    if not expected.keys() <= counters.attrib.keys():
        raise ValueError("Required result counters are missing.")
    try:
        actual = {name: int(value) for name, value in counters.attrib.items()}
    except (TypeError, ValueError):
        raise ValueError("Result counters must be nonnegative integers.") from None
    if any(value < 0 or value != expected.get(name, 0) for name, value in actual.items()):
        raise ValueError("Result counters do not match the complete passing test results.")

    definitions = root.findall(f"{NAMESPACE}TestDefinitions/{NAMESPACE}UnitTest")
    entries = root.findall(f"{NAMESPACE}TestEntries/{NAMESPACE}TestEntry")
    result_ids = {(result.get("testId"), result.get("executionId")) for result in results}
    definition_ids = {
        (definition.get("id"), execution.get("id"))
        for definition in definitions
        for execution in definition.findall(NAMESPACE + "Execution")
    }
    entry_ids = {(entry.get("testId"), entry.get("executionId")) for entry in entries}
    if len(definitions) != len(results) or len(entries) != len(results) or definition_ids != result_ids or entry_ids != result_ids:
        raise ValueError("Test definitions or entries do not match the executed results.")

    return len(results)


def main(paths):
    if not paths:
        print("Provide at least one explicit TRX report path.", file=sys.stderr)
        return 1

    failed = False
    for index, path in enumerate(paths, start=1):
        try:
            count = verify_report(path)
            print(f"Report {index}: verified {count} passing tests with unique identities.")
        except ValueError as error:
            # Do not print paths, test names, token-bearing URLs, or test output.
            print(f"Report {index}: {error}", file=sys.stderr)
            failed = True
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
