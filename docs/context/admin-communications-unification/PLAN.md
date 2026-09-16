# Implementation plan

1. Add a shared admin communication row model and communication-type classification.
2. Extend `/Admin/Communications` query handling to merge both sources, filter by type, date, and subject, then sort and paginate the combined rows.
3. Update the Razor table with type labels/icons, name, subject, date, payment, donation, and user information.
4. Add localized labels, filter options, empty-state text, and accessible icon text in the existing admin resource files.
5. Update integration tests for merged records, duplicate prevention, classification, and each filter.
6. Build the web project and run the focused integration test project.
