# Contract: Release Notes

Release notes are required for each completed user-facing release.

## Required Sections

- `Version`: Released version
- `Released At`: Publication timestamp
- `Summary`: Short user-readable overview
- `Changed Capabilities`: User-visible changes
- `Fixes`: Corrected behavior
- `Known Issues`: Known limitations or active issues
- `Operational Caveats`: Operator-facing deployment or runtime notes
- `Migration Notes`: Required user or operator actions, if any
- `Release Record`: Link or identifier for release history

## Publication Rules

- Release notes must be reviewed before publication.
- Completed user-facing releases must link published release notes from release history.
- Notes must not expose secrets, private deployment details, raw logs, or protected task details.
- If a section has no content, it must state `None` rather than being omitted.

## Example Skeleton

```markdown
# Release VERSION

**Released At**: TIMESTAMP
**Release Record**: RELEASE_RECORD_ID

## Summary

SUMMARY

## Changed Capabilities

- CHANGE

## Fixes

- FIX

## Known Issues

- None

## Operational Caveats

- CAVEAT

## Migration Notes

- None
```
