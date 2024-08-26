import { createContext, FC, useContext, useEffect, useState } from 'react'
import { useRobotContext } from './RobotContext'
import { AlertType } from './AlertContext'
import { SafeZoneAlertContent, SafeZoneAlertListContent } from 'components/Alerts/SafeZoneAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { RobotFlotillaStatus } from 'models/Robot'
import { useAlertContext } from './AlertContext'

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
    const { setAlert, clearAlert, setListAlert, clearListAlert } = useAlertContext()

    useEffect(() => {
        const missionQueueFozenStatus = enabledRobots.filter(
            (robot) => robot.flotillaStatus === RobotFlotillaStatus.SafeZone
        )

        if (missionQueueFozenStatus.length > 0 && safeZoneStatus === false) {
            setSafeZoneStatus((oldStatus) => !oldStatus)
            clearListAlert(AlertType.DismissSafeZone)
            clearAlert(AlertType.DismissSafeZone)
            setListAlert(
                AlertType.RequestSafeZone,
                <SafeZoneAlertListContent
                    alertType={AlertType.RequestSafeZone}
                    alertCategory={AlertCategory.WARNING}
                />,
                AlertCategory.WARNING
            )
            setAlert(
                AlertType.RequestSafeZone,
                <SafeZoneAlertContent alertType={AlertType.RequestSafeZone} alertCategory={AlertCategory.WARNING} />,
                AlertCategory.WARNING
            )
        } else if (missionQueueFozenStatus.length === 0 && safeZoneStatus === true) {
            setSafeZoneStatus((oldStatus) => !oldStatus)
            clearListAlert(AlertType.RequestSafeZone)
            clearListAlert(AlertType.SafeZoneSuccess)
            clearAlert(AlertType.RequestSafeZone)
            clearAlert(AlertType.SafeZoneSuccess)
            setListAlert(
                AlertType.DismissSafeZone,
                <SafeZoneAlertListContent alertType={AlertType.DismissSafeZone} alertCategory={AlertCategory.INFO} />,
                AlertCategory.INFO
            )
            setAlert(
                AlertType.DismissSafeZone,
                <SafeZoneAlertContent alertType={AlertType.DismissSafeZone} alertCategory={AlertCategory.INFO} />,
                AlertCategory.INFO
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
