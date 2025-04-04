import { createContext, FC, useEffect, useState } from 'react'
import { useRobotContext } from './RobotContext'
import { AlertType } from './AlertContext'
import { DockAlertContent, DockAlertListContent } from 'components/Alerts/DockAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { RobotFlotillaStatus } from 'models/Robot'
import { useAlertContext } from './AlertContext'

interface IDockContext {
    DockStatus: boolean
}

interface Props {
    children: React.ReactNode
}

const defaultDockInterface = {
    DockStatus: false,
}

const DockContext = createContext<IDockContext>(defaultDockInterface)

export const DockProvider: FC<Props> = ({ children }) => {
    const [DockStatus, setDockStatus] = useState<boolean>(defaultDockInterface.DockStatus)
    const { enabledRobots } = useRobotContext()
    const { setAlert, clearAlert, setListAlert, clearListAlert } = useAlertContext()

    useEffect(() => {
        const missionQueueFozenStatus = enabledRobots.filter(
            (robot) => robot.flotillaStatus === RobotFlotillaStatus.Home
        )

        if (missionQueueFozenStatus.length > 0 && DockStatus === false) {
            setDockStatus((oldStatus) => !oldStatus)
            clearListAlert(AlertType.DismissDock)
            clearAlert(AlertType.DismissDock)
            setListAlert(
                AlertType.RequestDock,
                <DockAlertListContent alertType={AlertType.RequestDock} alertCategory={AlertCategory.WARNING} />,
                AlertCategory.WARNING
            )
            setAlert(
                AlertType.RequestDock,
                <DockAlertContent alertType={AlertType.RequestDock} alertCategory={AlertCategory.WARNING} />,
                AlertCategory.WARNING
            )
        } else if (missionQueueFozenStatus.length === 0 && DockStatus === true) {
            setDockStatus((oldStatus) => !oldStatus)
            clearListAlert(AlertType.RequestDock)
            clearListAlert(AlertType.DockSuccess)
            clearAlert(AlertType.RequestDock)
            clearAlert(AlertType.DockSuccess)
            setListAlert(
                AlertType.DismissDock,
                <DockAlertListContent alertType={AlertType.DismissDock} alertCategory={AlertCategory.INFO} />,
                AlertCategory.INFO
            )
            setAlert(
                AlertType.DismissDock,
                <DockAlertContent alertType={AlertType.DismissDock} alertCategory={AlertCategory.INFO} />,
                AlertCategory.INFO
            )
        }
    }, [enabledRobots])

    return (
        <DockContext.Provider
            value={{
                DockStatus,
            }}
        >
            {children}
        </DockContext.Provider>
    )
}
