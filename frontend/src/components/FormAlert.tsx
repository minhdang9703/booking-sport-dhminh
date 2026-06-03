type FormAlertProps = {
  tone: 'error' | 'success'
  message: string
}

export function FormAlert({ tone, message }: FormAlertProps) {
  const className =
    tone === 'error'
      ? 'border-[#ffdad6] bg-[#ffdad6] text-[#93000a]'
      : 'border-[#d6f5df] bg-[#e9f9ef] text-[#005321]'

  return (
    <div className={`rounded-xl border px-4 py-3 text-sm font-medium ${className}`}>
      {message}
    </div>
  )
}
