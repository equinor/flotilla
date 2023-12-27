import { createContext, FC, ReactNode, useContext, useEffect, useState } from 'react'
import { addMinutes, max } from 'date-fns'
import { Mission, MissionStatus } from 'models/Mission'
import { FailedMissionAlertContent } from 'components/Alerts/FailedMissionAlert'
import { BackendAPICaller } from 'api/ApiCaller'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { useInstallationContext } from './InstallationContext'
import { Alert } from 'models/Alert'
import { FailedSafeZoneAlertContent } from 'components/Alerts/FailedSafeZoneAlertContent'
import { useRobotContext } from './RobotContext'

type AlertDictionaryType = { [key in AlertType]?: { content: ReactNode | undefined; dismissFunction: () => void } }

export enum AlertType {
    MissionFail,
    RequestFail,
    SafeZoneFail,
}

const alertTypeEnumMap: { [key: string]: AlertType } = {
    safezoneFailure: AlertType.SafeZoneFail,
}

interface IAlertContext {
    alerts: AlertDictionaryType
    setAlert: (source: AlertType, alert: ReactNode) => void
    clearAlerts: () => void
    clearAlert: (source: AlertType) => void
}

interface Props {
    children: React.ReactNode
}

const defaultAlertInterface = {
    alerts: {},
    setAlert: (source: AlertType, alert: ReactNode) => {},
    clearAlerts: () => {},
    clearAlert: (source: AlertType) => {},
}

export const AlertContext = createContext<IAlertContext>(defaultAlertInterface)

export const AlertProvider: FC<Props> = ({ children }) => {
    const [alerts, setAlerts] = useState<AlertDictionaryType>(defaultAlertInterface.alerts)
    const [recentFailedMissions, setRecentFailedMissions] = useState<Mission[]>([])
    const { registerEvent, connectionReady } = useSignalRContext()
    const { installationCode } = useInstallationContext()
    const { enabledRobots } = useRobotContext()

    const pageSize: number = 100
    // The default amount of minutes in the past for failed missions to generate an alert
    const defaultTimeInterval: number = 10
    // The maximum amount of minutes in the past for failed missions to generate an alert
    const maxTimeInterval: number = 60
    const dismissMissionFailTimeKey: string = 'lastMissionFailDismissalTime'

    const setAlert = (source: AlertType, alert: ReactNode) =>
        setAlerts({ ...alerts, [source]: { content: alert, dismissFunction: () => clearAlert(source) } })

    const clearAlerts = () => setAlerts({})

    const clearAlert = (source: AlertType) => {
        if (source === AlertType.MissionFail)
            sessionStorage.setItem(dismissMissionFailTimeKey, JSON.stringify(Date.now()))
        let newAlerts = { ...alerts }
        delete newAlerts[source]
        setAlerts(newAlerts)
    }

    const getLastDismissalTime = (): Date => {
        const sessionValue = sessionStorage.getItem(dismissMissionFailTimeKey)
        if (sessionValue === null || sessionValue === '') {
            return addMinutes(Date.now(), -defaultTimeInterval)
        } else {
            // If last dismissal time was more than {MaxTimeInterval} minutes ago, use the limit value instead
            return max([addMinutes(Date.now(), -maxTimeInterval), JSON.parse(sessionValue)])
        }
    }

    // This variable is needed since the state in the useEffect below uses an outdated alert object
    const [newFailedMissions, setNewFailedMissions] = useState<Mission[]>([])

    // Set the initial failed missions when loading the page or changing installations
    useEffect(() => {
        const updateRecentFailedMissions = () => {
            const lastDismissTime: Date = getLastDismissalTime()
            BackendAPICaller.getMissionRuns({ statuses: [MissionStatus.Failed], pageSize: pageSize }).then(
                (missions) => {
                    const newRecentFailedMissions = missions.content.filter(
                        (m) =>
                            new Date(m.endTime!) > lastDismissTime &&
                            (!installationCode ||
                                m.installationCode!.toLocaleLowerCase() !== installationCode.toLocaleLowerCase())
                    )
                    if (newRecentFailedMissions.length > 0) setNewFailedMissions(newRecentFailedMissions)
                    setRecentFailedMissions(newRecentFailedMissions)
                }
            )
        }
        if (!recentFailedMissions || recentFailedMissions.length === 0) updateRecentFailedMissions()
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [installationCode])

    // Register a signalR event handler that listens for new failed missions
    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.missionRunFailed, (username: string, message: string) => {
                const newFailedMission: Mission = JSON.parse(message)
                const lastDismissTime: Date = getLastDismissalTime()

                setRecentFailedMissions((failedMissions) => {
                    if (
                        installationCode &&
                        (!newFailedMission.installationCode ||
                            newFailedMission.installationCode.toLocaleLowerCase() !==
                                installationCode.toLocaleLowerCase())
                    )
                        return failedMissions // Ignore missions for other installations
                    // Ignore missions shortly after the user dismissed the last one
                    if (new Date(newFailedMission.endTime!) <= lastDismissTime) return failedMissions
                    let isDuplicate = failedMissions.filter((m) => m.id === newFailedMission.id).length > 0
                    if (isDuplicate) return failedMissions // Ignore duplicate failed missions
                    return [...failedMissions, newFailedMission]
                })
            })
            registerEvent(SignalREventLabels.alert, (username: string, message: string) => {
                const backendAlert: Alert = JSON.parse(message)
                const alertType = alertTypeEnumMap[backendAlert.alertCode]

                if (backendAlert.robotId !== null) {
                    const relevantRobots = enabledRobots.filter((r) => r.id === backendAlert.robotId)
                    if (!relevantRobots) return
                    const relevantRobot = relevantRobots[0]
                    if (relevantRobot.currentInstallation.installationCode !== installationCode) return

                    // Here we could update the robot state manually, but this is best done on the backend
                }

                setAlert(alertType, <FailedSafeZoneAlertContent message={backendAlert.alertDescription} />)
            })
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [registerEvent, connectionReady, installationCode])

    useEffect(() => {
        if (newFailedMissions.length > 0) {
            setAlert(AlertType.MissionFail, <FailedMissionAlertContent missions={newFailedMissions} />)
            setNewFailedMissions([])
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [newFailedMissions])

    return (
        <AlertContext.Provider
            value={{
                alerts,
                setAlert,
                clearAlerts,
                clearAlert,
            }}
        >
            {children}
        </AlertContext.Provider>
    )
}

export const useAlertContext = () => useContext(AlertContext)
