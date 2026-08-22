import Link from "next/link";

type BrandProps = {
  compact?: boolean;
};

export function Brand({ compact = false }: BrandProps) {
  return (
    <Link className="brand" href="/dashboard" aria-label="CommerceFlow — início">
      <span className="brand-mark" aria-hidden="true">
        <span />
        <span />
        <span />
      </span>
      {!compact && (
        <span className="brand-copy">
          <strong>CommerceFlow</strong>
          <small>Operations Console</small>
        </span>
      )}
    </Link>
  );
}
