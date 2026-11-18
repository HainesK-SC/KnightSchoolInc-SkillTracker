type IconProps = {
  className?: string;
};

type NamedIconProps = IconProps & {
  name: string;
};

function SwordIcon({ className }: IconProps) {
  return (
    <svg
      className={className}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      focusable="false"
    >
      <path d="M14 4 21 2l-2 7-9 9-4-4 8-10Z" />
      <path d="m7 13 4 4" />
      <path d="m5 17 2 2" />
      <path d="m3 21 4-4" />
    </svg>
  );
}

function CrossedSwordsIcon({ className }: IconProps) {
  return (
    <svg
      className={className}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      focusable="false"
    >
      <path d="m4 3 6 6" />
      <path d="m14 13 6 6" />
      <path d="m3 4 2-1 1 2" />
      <path d="m18 17 3 3" />
      <path d="m20 3-6 6" />
      <path d="m10 13-6 6" />
      <path d="m21 4-2-1-1 2" />
      <path d="m6 17-3 3" />
      <circle cx="12" cy="11" r="2" />
    </svg>
  );
}

function ShieldIcon({ className }: IconProps) {
  return (
    <svg
      className={className}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      focusable="false"
    >
      <path d="M12 2 20 5v6c0 5-3.3 8.6-8 11-4.7-2.4-8-6-8-11V5l8-3Z" />
      <path d="M12 6v11" />
      <path d="M7.5 10h9" />
    </svg>
  );
}

function HelmetIcon({ className }: IconProps) {
  return (
    <svg
      className={className}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      focusable="false"
    >
      <path d="M5 20v-8a7 7 0 0 1 14 0v8" />
      <path d="M5 13h14" />
      <path d="M9 13v7" />
      <path d="M15 13v7" />
      <path d="M9 17h6" />
      <path d="M12 5v4" />
    </svg>
  );
}

function GauntletIcon({ className }: IconProps) {
  return (
    <svg
      className={className}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      focusable="false"
    >
      <path d="M6 11V7a2 2 0 0 1 4 0v3" />
      <path d="M10 10V5a2 2 0 0 1 4 0v5" />
      <path d="M14 10V6a2 2 0 0 1 4 0v7" />
      <path d="M6 10a2 2 0 0 0-4 1v3c0 4.4 3.6 8 8 8h2c4.4 0 8-3.6 8-8v-3a2 2 0 0 0-4 0v2" />
      <path d="M7 16h9" />
    </svg>
  );
}

function MovementIcon({ className }: IconProps) {
  return (
    <svg
      className={className}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      focusable="false"
    >
      <circle cx="13" cy="4" r="2" />
      <path d="m10 9 3-2 3 3" />
      <path d="m13 7-2 6 4 3" />
      <path d="m11 13-4 3" />
      <path d="m15 16 3 5" />
      <path d="m7 16-3 4" />
      <path d="M3 8h5" />
      <path d="M2 11h5" />
    </svg>
  );
}

function StarIcon({ className }: IconProps) {
  return (
    <svg
      className={className}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      focusable="false"
    >
      <path d="m12 2 2.8 6.4 7 .7-5.3 4.7 1.5 6.9-6-3.6-6 3.6 1.5-6.9-5.3-4.7 7-.7L12 2Z" />
    </svg>
  );
}

export function SkillIcon({
  name,
  className,
}: NamedIconProps) {
  const normalizedName = name.toLowerCase();

  if (
    normalizedName.includes("shield") ||
    normalizedName.includes("guard") ||
    normalizedName.includes("block")
  ) {
    return <ShieldIcon className={className} />;
  }

  if (
    normalizedName.includes("armour") ||
    normalizedName.includes("armor") ||
    normalizedName.includes("helmet")
  ) {
    return <HelmetIcon className={className} />;
  }

  if (
    normalizedName.includes("punch") ||
    normalizedName.includes("unarmed") ||
    normalizedName.includes("combat stance")
  ) {
    return <GauntletIcon className={className} />;
  }

  if (
    normalizedName.includes("fall") ||
    normalizedName.includes("movement")
  ) {
    return <MovementIcon className={className} />;
  }

  if (
    normalizedName.includes("parry") ||
    normalizedName.includes("cross")
  ) {
    return <CrossedSwordsIcon className={className} />;
  }

  if (
    normalizedName.includes("riposte") ||
    normalizedName.includes("sword") ||
    normalizedName.includes("strike")
  ) {
    return <SwordIcon className={className} />;
  }

  return <StarIcon className={className} />;
}

export function DisciplineIcon({
  name,
  className,
}: NamedIconProps) {
  const normalizedName = name.toLowerCase();

  if (
    normalizedName.includes("sword") ||
    normalizedName.includes("fencing") ||
    normalizedName.includes("blade")
  ) {
    return <CrossedSwordsIcon className={className} />;
  }

  if (
    normalizedName.includes("armour") ||
    normalizedName.includes("armor") ||
    normalizedName.includes("shield")
  ) {
    return <HelmetIcon className={className} />;
  }

  if (
    normalizedName.includes("unarmed") ||
    normalizedName.includes("combat")
  ) {
    return <GauntletIcon className={className} />;
  }

  return <ShieldIcon className={className} />;
}