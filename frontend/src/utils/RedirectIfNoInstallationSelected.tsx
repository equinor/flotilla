import { useAssetContext } from 'components/Contexts/RobotContext'
import { config } from 'config'

export function redirectIfNoInstallationSelected() {
    const { installationCode, installationName } = useAssetContext()
    if (installationCode === '' && installationName === '') {
        window.location.href = `${config.FRONTEND_BASE_ROUTE}/`
    }
}
