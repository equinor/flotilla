import { createContext, FC, useContext, useEffect, useState, useCallback } from 'react'
import { addMinutes, max } from 'date-fns'
import { Mission, MissionStatus } from 'models/Mission'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { BackendAlert, Alert, AlertSeverity } from 'models/Alert'
import { useAssetContext } from './AssetContext'
import { convertUTCDateToLocalDate } from 'utils/StringFormatting'
import { useLanguageContext } from './LanguageContext'
import { useBackendApi } from 'api/UseBackendApi'
import { AuthContext } from './AuthContext'
import { InstallationContext } from './InstallationContext'

interface IAlertContext {
    banner: Alert | null
    setBanner: (message: string, severity: AlertSeverity, title?: string) => void
    clearBanner: () => void

    notifications: Alert[]
    addNotification: (message: string, severity: AlertSeverity, title?: string, missionId?: string) => void
    removeNotification: (index: number) => void
    clearAllNotifications: () => void
}

interface Props {
    children: React.ReactNode
}

const defaultAlertInterface: IAlertContext = {
    banner: null,
    setBanner: () => {},
    clearBanner: () => {},
    notifications: [],
    addNotification: () => {},
    removeNotification: () => {},
    clearAllNotifications: () => {},
}

const AlertContext = createContext<IAlertContext>(defaultAlertInterface)

export const AlertProvider: FC<Props> = ({ children }) => {
    const [banner, setBannerState] = useState<Alert | null>(null)
    const [notifications, setNotificationsState] = useState<Alert[]>(
        JSON.parse(localStorage.getItem('flotilla_notifications') || '[]')
    )
    const [recentFailedMissions, setRecentFailedMissions] = useState<Mission[]>([])
    const { registerEvent, connectionReady } = useSignalRContext()
    const { TranslateText } = useLanguageContext()
    const { enabledRobots } = useAssetContext()
    const { installation } = useContext(InstallationContext)
    const backendApi = useBackendApi()
    const { isAuthenticated } = useContext(AuthContext)

    // Persist notifications to localStorage
    useEffect(() => {
        localStorage.setItem('flotilla_notifications', JSON.stringify(notifications))
    }, [notifications])

    const pageSize: number = 100
    // The default amount of minutes in the past for failed missions to generate an alert
    const defaultTimeInterval: number = 10
    // The maximum amount of minutes in the past for failed missions to generate an alert
    const maxTimeInterval: number = 60
    const dismissMissionFailTimeKey: string = 'lastMissionFailDismissalTime'

    const setBanner = useCallback((message: string, severity: AlertSeverity, title?: string) => {
        setBannerState({ message, severity, title })
    }, [])

    const clearBanner = useCallback(() => setBannerState(null), [])

    const addNotification = useCallback(
        (message: string, severity: AlertSeverity, title?: string, missionId?: string) => {
            const newNotification: Alert = { message, severity, title, missionId }
            setNotificationsState((prev) => [...prev, newNotification])
        },
        []
    )

    const removeNotification = useCallback((index: number) => {
        setNotificationsState((prev) => prev.filter((_, i) => i !== index))
    }, [])

    const clearAllNotifications = useCallback(() => setNotificationsState([]), [])

    const getLastDismissalTime = (): Date => {
        const sessionValue = sessionStorage.getItem(dismissMissionFailTimeKey)
        if (sessionValue === null || sessionValue === '') {
            return addMinutes(Date.now(), -defaultTimeInterval)
        } else {
            // If last dismissal time was more than {MaxTimeInterval} minutes ago, use the limit value instead
            return max([addMinutes(Date.now(), -maxTimeInterval), JSON.parse(sessionValue)])
        }
    }

    // Set the initial failed missions when loading the page or changing installations
    useEffect(() => {
        if (!isAuthenticated) return
        const updateRecentFailedMissions = () => {
            const lastDismissTime: Date = getLastDismissalTime()
            backendApi
                .getMissionRuns({
                    installationCode: installation.installationCode,
                    statuses: [MissionStatus.Failed],
                    pageSize: pageSize,
                })
                .then((missions) => {
                    const newRecentFailedMissions = missions.content.filter(
                        (m) =>
                            convertUTCDateToLocalDate(new Date(m.endTime!)) > lastDismissTime &&
                            m.installationCode!.toLocaleLowerCase() ===
                                installation.installationCode.toLocaleLowerCase()
                    )
                    setRecentFailedMissions(newRecentFailedMissions)
                })
                .catch(() => {
                    setBanner(TranslateText('Failed to retrieve failed missions'), 'error')
                    addNotification(TranslateText('Failed to retrieve failed missions'), 'error')
                })
        }
        if (!recentFailedMissions || recentFailedMissions.length === 0) updateRecentFailedMissions()
    }, [installation, isAuthenticated])

    // Register a signalR event handler that listens for new failed missions
    useEffect(() => {
        if (!connectionReady) return
        return registerEvent(SignalREventLabels.missionRunFailed, (username: string, message: string) => {
            const newFailedMission: Mission = JSON.parse(message)
            const lastDismissTime: Date = getLastDismissalTime()

            setRecentFailedMissions((failedMissions) => {
                if (
                    !newFailedMission.installationCode ||
                    newFailedMission.installationCode.toLocaleLowerCase() !==
                        installation.installationCode.toLocaleLowerCase()
                )
                    return failedMissions // Ignore missions for other installations
                // Ignore missions shortly after the user dismissed the last one
                if (convertUTCDateToLocalDate(new Date(newFailedMission.endTime!)) <= lastDismissTime)
                    return failedMissions
                const isDuplicate = failedMissions.filter((m) => m.id === newFailedMission.id).length > 0
                if (isDuplicate) return failedMissions // Ignore duplicate failed missions
                return [...failedMissions, newFailedMission]
            })
        })
    }, [registerEvent, connectionReady, installation])

    // Register a signalR event handler for general alerts from backend
    useEffect(() => {
        if (!connectionReady) return
        return registerEvent(SignalREventLabels.alert, (username: string, message: string) => {
            const backendAlert: BackendAlert = JSON.parse(message)
            if (backendAlert.installationCode.toLocaleLowerCase() !== installation.installationCode.toLocaleLowerCase())
                return

            if (backendAlert.robotId !== null && !enabledRobots.filter((r) => r.id === backendAlert.robotId)) return

            // Route alerts based on alertCode to banner or notification
            switch (backendAlert.alertCode) {
                case 'AutoScheduleFail':
                    // AutoScheduleFail -> Notification (persistent)
                    addNotification(backendAlert.alertMessage, 'error', backendAlert.alertTitle)
                    break
                case 'skipAutoMission':
                    // InfoAlert -> Banner (transient)
                    setBanner(backendAlert.alertMessage, 'info', backendAlert.alertTitle)
                    break
                case 'generalFailure':
                    // This comes from MQTT (mission failures handled separately), but show as banner
                    setBanner(backendAlert.alertMessage, 'warning', backendAlert.alertTitle)
                    break
                case 'DockFailure':
                    // DockFailure -> Both banner and notification
                    setBanner(backendAlert.alertMessage, 'error', backendAlert.alertTitle)
                    addNotification(backendAlert.alertMessage, 'error', backendAlert.alertTitle)
                    break
                default:
                    // Unknown alert codes default to banner
                    setBanner(backendAlert.alertMessage, 'warning', backendAlert.alertTitle)
            }
        })
    }, [registerEvent, connectionReady, installation, enabledRobots])

    // Update banner and notifications to show failed missions
    useEffect(() => {
        if (recentFailedMissions.length > 0) {
            const missionList = recentFailedMissions.map((m) => m.name).join(', ')
            // Use setTimeout to defer state update and avoid cascading render warnings
            const timer = setTimeout(() => {
                setBanner(`Mission failed: ${missionList}`, 'error')
                // Add to notifications with mission ID for linking
                recentFailedMissions.forEach((mission) => {
                    addNotification(`Mission failed: ${mission.name}`, 'error', undefined, mission.id)
                })
            }, 0)
            return () => clearTimeout(timer)
        }
    }, [recentFailedMissions, setBanner, addNotification])

    return (
        <AlertContext.Provider
            value={{
                banner,
                setBanner,
                clearBanner,
                notifications,
                addNotification,
                removeNotification,
                clearAllNotifications,
            }}
        >
            {children}
        </AlertContext.Provider>
    )
}

export const useAlertContext = () => useContext(AlertContext)
