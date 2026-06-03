type FormFieldProps = {
  label: string
  name: string
  type?: string
  value: string
  placeholder: string
  autoComplete?: string
  required?: boolean
  onChange: (value: string) => void
}

export function FormField({
  label,
  name,
  type = 'text',
  value,
  placeholder,
  autoComplete,
  required,
  onChange,
}: FormFieldProps) {
  return (
    <label className="block">
      <span className="text-sm font-semibold text-[#191c1e]">{label}</span>
      <input
        name={name}
        type={type}
        value={value}
        placeholder={placeholder}
        autoComplete={autoComplete}
        required={required}
        onChange={(event) => onChange(event.target.value)}
        className="mt-2 w-full rounded-xl border border-[#bccbb9] bg-white px-4 py-3 text-base text-[#191c1e] outline-none transition placeholder:text-slate-400 focus:border-[#006e2f] focus:ring-2 focus:ring-[#006e2f]/15"
      />
    </label>
  )
}
