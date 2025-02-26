import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { config } from 'config'

export function redirectIfNoInstallationSelected() {
    const { installationCode, installationName } = useInstallationContext()
    if (installationCode === '' && installationName === '') {
        window.location.href = `${config.FRONTEND_BASE_ROUTE}/`
    }
}
