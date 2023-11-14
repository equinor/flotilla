import { createContext, FC, useContext, useEffect, useState } from 'react'
import { useRobotContext } from './RobotContext'
import { useInstallationContext } from './InstallationContext'
import { AlertType, useAlertContext } from './AlertContext'
import { SafeZoneBanner } from 'components/Pages/FrontPage/MissionOverview/SafeZoneBanner'
import { AlertCategory } from 'components/Alerts/AlertsBanner'

interface ISafeZoneContext {
    safeZoneStatus: boolean
}

interface Props {
    children: React.ReactNode
}

const defaultSafeZoneInterface = {
    safeZoneStatus: false,
}

export const SafeZoneContext = createContext<ISafeZoneContext>(defaultSafeZoneInterface)

export const SafeZoneProvider: FC<Props> = ({ children }) => {
    const [safeZoneStatus, setSafeZoneStatus] = useState<boolean>(defaultSafeZoneInterface.safeZoneStatus)
    const { enabledRobots } = useRobotContext()
    const { installationCode } = useInstallationContext()
    const { setAlert, clearAlert } = useAlertContext()

    useEffect(() => {
        const missionQueueFozenStatus = enabledRobots
            .filter(
                (robot) =>
                    robot.currentInstallation.installationCode.toLocaleLowerCase() ===
                    installationCode.toLocaleLowerCase()
            )
            .map((robot) => robot.missionQueueFrozen)
            .filter((status) => status === true)

        if (missionQueueFozenStatus.length > 0 && safeZoneStatus === false) {
            setSafeZoneStatus((oldStatus) => !oldStatus)
            clearAlert(AlertType.DismissSafeZone)
            setAlert(
                AlertType.RequestSafeZone,
                <SafeZoneBanner alertCategory={AlertCategory.WARNING} />,
                AlertCategory.WARNING
            )
        } else if (missionQueueFozenStatus.length === 0 && safeZoneStatus === true) {
            setSafeZoneStatus((oldStatus) => !oldStatus)
            clearAlert(AlertType.RequestSafeZone)
            setAlert(
                AlertType.DismissSafeZone,
                <SafeZoneBanner alertCategory={AlertCategory.SUCCESS} />,
                AlertCategory.SUCCESS
            )
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [enabledRobots])

    return (
        <SafeZoneContext.Provider
            value={{
                safeZoneStatus,
            }}
        >
            {children}
        </SafeZoneContext.Provider>
    )
}

export const useSafeZoneContext = () => useContext(SafeZoneContext)
