import { useRobotContext } from 'components/Contexts/RobotContext'
import { config } from 'config'

export function redirectIfNoInstallationSelected() {
    const { installationCode, installationName } = useRobotContext()
    if (installationCode === '' && installationName === '') {
        window.location.href = `${config.FRONTEND_BASE_ROUTE}/`
    }
}
