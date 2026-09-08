import { createContext, FC, useContext, useEffect, useState, useCallback, useRef } from 'react'
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
    banner: Alert | undefined
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
    banner: undefined,
    setBanner: () => {},
    clearBanner: () => {},
    notifications: [],
    addNotification: () => {},
    removeNotification: () => {},
    clearAllNotifications: () => {},
}

const AlertContext = createContext<IAlertContext>(defaultAlertInterface)

const notificationsStorageKey: string = 'flotilla_notifications'
const dismissMissionFailTimeKey: string = 'lastMissionFailDismissalTime'
const notifiedFailedMissionsKey: string = 'notifiedFailedMissionIds'

const readPersistedNotifications = (): Alert[] => {
    try {
        return JSON.parse(localStorage.getItem(notificationsStorageKey) || '[]')
    } catch {
        return []
    }
}

const readNotifiedMissionIds = (): Set<string> => {
    try {
        return new Set<string>(JSON.parse(sessionStorage.getItem(notifiedFailedMissionsKey) || '[]'))
    } catch {
        return new Set<string>()
    }
}

type Translator = (str: string, args?: string[]) => string

const describeFailedMission = (mission: Mission, TranslateText: Translator): string => {
    const prefix = `${mission.name} ${TranslateText('failed on robot')} ${mission.robot.name}`
    return mission.statusReason ? `${prefix}: ${mission.statusReason}` : prefix
}

const describeFailedMissions = (missions: Mission[], TranslateText: Translator): string => {
    if (missions.length === 1) return describeFailedMission(missions[0], TranslateText)
    return `${missions.length} ${TranslateText("missions failed recently. See 'Mission History' for more information.")}`
}

export const AlertProvider: FC<Props> = ({ children }) => {
    const [banner, setBannerState] = useState<Alert | undefined>(undefined)
    const [notifications, setNotificationsState] = useState<Alert[]>(readPersistedNotifications)
    const [recentFailedMissions, setRecentFailedMissions] = useState<Mission[]>([])
    const { registerEvent, connectionReady } = useSignalRContext()
    const { TranslateText } = useLanguageContext()
    const { enabledRobots } = useAssetContext()
    const { installation } = useContext(InstallationContext)
    const backendApi = useBackendApi()
    const { isAuthenticated } = useContext(AuthContext)

    const notifiedMissionIds = useRef<Set<string>>(readNotifiedMissionIds())

    // Persisting from an effect rather than from each of the call sites below keeps the
    // state updaters pure, which StrictMode and the React compiler both require.
    useEffect(() => {
        localStorage.setItem(notificationsStorageKey, JSON.stringify(notifications))
    }, [notifications])

    const pageSize: number = 100
    const defaultMinutesForFailedMissionsAlert: number = 10
    const maxMinutesForFailedMissionsAlert: number = 60

    const setBanner = useCallback((message: string, severity: AlertSeverity, title?: string) => {
        setBannerState({ message, severity, title })
    }, [])

    const clearBanner = useCallback(() => {
        // Dismissing the failed mission banner suppresses those same failures from being
        // raised again, both by the fetch on page load and by the SignalR handler.
        if (banner?.isMissionFailure) {
            sessionStorage.setItem(dismissMissionFailTimeKey, JSON.stringify(Date.now()))
            setRecentFailedMissions([])
        }
        setBannerState(undefined)
    }, [banner])

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
            return addMinutes(Date.now(), -defaultMinutesForFailedMissionsAlert)
        } else {
            return max([addMinutes(Date.now(), -maxMinutesForFailedMissionsAlert), JSON.parse(sessionValue)])
        }
    }

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
        // Same guard, and so the same missing dependency, as the mission run fetch in
        // MissionRunsContext: without isAuthenticated the early return above is never
        // reconsidered once authentication completes.
    }, [installation, isAuthenticated])

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
                    return failedMissions
                if (convertUTCDateToLocalDate(new Date(newFailedMission.endTime!)) <= lastDismissTime)
                    return failedMissions
                const isDuplicate = failedMissions.filter((m) => m.id === newFailedMission.id).length > 0
                if (isDuplicate) return failedMissions // Ignore duplicate failed missions
                return [...failedMissions, newFailedMission]
            })
        })
    }, [registerEvent, connectionReady, installation])

    useEffect(() => {
        if (!connectionReady) return
        return registerEvent(SignalREventLabels.alert, (username: string, message: string) => {
            const backendAlert: BackendAlert = JSON.parse(message)
            if (backendAlert.installationCode.toLocaleLowerCase() !== installation.installationCode.toLocaleLowerCase())
                return

            // The backend may send null rather than omitting the field, so check truthiness.
            if (backendAlert.robotId && !enabledRobots.some((r) => r.id === backendAlert.robotId)) return

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

    useEffect(() => {
        if (recentFailedMissions.length === 0) return

        // Use setTimeout to defer state update and avoid cascading render warnings
        const timer = setTimeout(() => {
            setBannerState({
                title: TranslateText(MissionStatus.Failed),
                message: describeFailedMissions(recentFailedMissions, TranslateText),
                severity: 'error',
                isMissionFailure: true,
            })

            // Only notify about missions we have not already notified about. Without this the
            // effect re-adds a notification for every known failure each time a new one arrives.
            const newMissions = recentFailedMissions.filter((m) => !notifiedMissionIds.current.has(m.id))
            if (newMissions.length === 0) return

            newMissions.forEach((m) => notifiedMissionIds.current.add(m.id))
            sessionStorage.setItem(notifiedFailedMissionsKey, JSON.stringify([...notifiedMissionIds.current]))

            setNotificationsState((prev) => [
                ...prev,
                ...newMissions.map((mission): Alert => ({
                    title: TranslateText(MissionStatus.Failed),
                    message: describeFailedMission(mission, TranslateText),
                    severity: 'error',
                    missionId: mission.id,
                })),
            ])
        }, 0)
        return () => clearTimeout(timer)
    }, [recentFailedMissions, TranslateText])

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
