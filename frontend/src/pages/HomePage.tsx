import { PagePlaceholder } from '../components/PagePlaceholder'
import { apiBaseUrl } from '../lib/apiClient'

export function HomePage() {
  return (
    <PagePlaceholder
      title="Home"
      description={`Frontend shell is ready. API base URL: ${apiBaseUrl}`}
    />
  )
}
