# Database Migration and Seeding

This folder now has two EF-aligned migration entry points:

- `20260307_ef_workflow_aligned_schema.sql`
  - Full schema from zero to latest model.
  - Use this for fresh database setup.
- `20260305_ef_model_baseline.sql`
  - Baseline schema only (`20260305154000_20260305_ModelBaseline`).
  - Use this only when you explicitly need baseline-only state.
- `20260307_ef_upgrade_workflow_extensions.sql`
  - Upgrade script from baseline `20260305154000_20260305_ModelBaseline` to workflow extensions.
  - Use this when database is already at baseline.

The patch script below is idempotent and can be applied after either path:

- `20260307_admin_feature_extensions.sql`
  - Ensures admin workflow tables and indexes exist.
  - Adds default row for `school_settings` when empty.
- `20260312_gap_closure_features.sql`
  - Adds enrollment approval/waitlist fields.
  - Adds `school_settings.default_grade_level_ids`.
  - Adds unique constraints for section name (`school_year + grade + name`) and subject code.
  - Adds enrollment waitlist/approval indexes.
- `20260409_structural_governance_framework.sql`
  - Adds structural governance tables:
    - `enrollment_state_transitions`
    - `governed_operation_logs`
    - `exception_queue_items`
  - Supports policy-driven enrollment transitions, cross-log traceability, and exception queue workflows.

## Suggested Run Order

Fresh database:

1. Run `20260307_ef_workflow_aligned_schema.sql`
2. Run `20260307_admin_feature_extensions.sql`
3. Run `20260312_gap_closure_features.sql`
4. Run `20260409_structural_governance_framework.sql`
5. Run `../Seeds/20260227_seed_school_sms.sql`

Existing baseline database:

1. Run `20260307_ef_upgrade_workflow_extensions.sql`
2. Run `20260307_admin_feature_extensions.sql`
3. Run `20260312_gap_closure_features.sql`
4. Run `20260409_structural_governance_framework.sql`
5. Run `../Seeds/20260227_seed_school_sms.sql`

## Notes

- Do not run legacy `20260227_batch*.sql` scripts for new setups; they predate the EF migration pipeline.
- `20260307_admin_feature_extensions.sql` intentionally does not hardcode `USE <database>`; run it against the currently selected connection/database.
