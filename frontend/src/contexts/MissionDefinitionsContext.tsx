import { createContext, FC, useContext, useEffect, useMemo, useState } from 'react'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { unsubscribeAll } from 'utils/signalR'
import { MissionDefinition } from 'models/MissionDefinition'
import { useLanguageContext } from './LanguageContext'
import { useAlertContext } from './AlertContext'
import { useBackendApi } from 'api/UseBackendApi'
import { InstallationContext } from './InstallationContext'
import { useOnPageVisible } from 'hooks/usePageVisibility'
import { useScheduledMissionRuns } from 'hooks/useScheduledMissionRuns'

interface IMissionDefinitionsContext extends ReturnType<typeof useScheduledMissionRuns> {
    missionDefinitions: MissionDefinition[]
}

interface Props {
    children: React.ReactNode
}

const defaultMissionDefinitionsContext: IMissionDefinitionsContext = {
    missionDefinitions: [],
    scheduledMissionRuns: [],
    autoScheduleLoading: true,
    autoScheduleError: false,
    refreshScheduledRuns: () => {},
}

const MissionDefinitionsContext = createContext<IMissionDefinitionsContext>(defaultMissionDefinitionsContext)

const upsertMissionDefinition = (oldQueue: MissionDefinition[], updatedMission: MissionDefinition) => {
    const oldQueueCopy = [...oldQueue]
    const existingIndex = oldQueueCopy.findIndex((m) => m.id === updatedMission.id)
    if (existingIndex !== -1) {
        oldQueueCopy[existingIndex] = updatedMission
        return oldQueueCopy
    } else {
        return [...oldQueueCopy, updatedMission]
    }
}

const useMissionDefinitions = (): IMissionDefinitionsContext => {
    const [missionDefinitions, setMissionDefinitions] = useState<MissionDefinition[]>([])
    const { registerEvent, connectionReady } = useSignalRContext()
    const { installation } = useContext(InstallationContext)
    const occurrences = useScheduledMissionRuns(installation.installationCode)
    const { TranslateText } = useLanguageContext()
    const { setBanner } = useAlertContext()
    const backendApi = useBackendApi()

    useEffect(() => {
        if (!connectionReady) return
        return unsubscribeAll([
            registerEvent(SignalREventLabels.missionDefinitionUpdated, (username: string, message: string) => {
                const missionDefinition: MissionDefinition = JSON.parse(message)
                setMissionDefinitions((oldMissionDefinitions) =>
                    upsertMissionDefinition(oldMissionDefinitions, missionDefinition)
                )
            }),
            registerEvent(SignalREventLabels.missionDefinitionCreated, (username: string, message: string) => {
                const missionDefinition: MissionDefinition = JSON.parse(message)
                setMissionDefinitions((oldMissionDefinitions) =>
                    upsertMissionDefinition(oldMissionDefinitions, missionDefinition)
                )
            }),
            registerEvent(SignalREventLabels.missionDefinitionDeleted, (username: string, message: string) => {
                const mDef: MissionDefinition = JSON.parse(message)
                setMissionDefinitions((oldMissionDefs) => {
                    const oldListCopy = [...oldMissionDefs]
                    const queueIndex = oldListCopy.findIndex((m) => m.id === mDef.id)
                    if (queueIndex !== -1) oldListCopy.splice(queueIndex, 1) // Remove deleted mission definition
                    return oldListCopy
                })
            }),
        ])
    }, [registerEvent, connectionReady])

    const fetchAndUpdateMissionDefinitions = () => {
        backendApi
            .getMissionDefinitions({
                installationCode: installation.installationCode,
                pageSize: 100,
                orderBy: 'InstallationCode installationCode',
            })
            .then((response) => {
                const missionDefinitionsInInstallation = response.content
                setMissionDefinitions(missionDefinitionsInInstallation ?? [])
            })
            .catch(() => {
                const errorMessage = TranslateText('Failed to retrieve inspection plans')
                setBanner(errorMessage, 'error')
            })
    }

    useEffect(() => {
        fetchAndUpdateMissionDefinitions()
    }, [installation])

    useOnPageVisible(() => {
        fetchAndUpdateMissionDefinitions()
    })

    const filteredMissionDefinitions = useMemo(
        () =>
            missionDefinitions.filter(
                (m) => m.installationCode.toLowerCase() === installation.installationCode.toLowerCase()
            ),
        [missionDefinitions, installation.installationCode]
    )

    return { missionDefinitions: filteredMissionDefinitions, ...occurrences }
}

export const MissionDefinitionsProvider: FC<Props> = ({ children }) => {
    const value = useMissionDefinitions()
    return <MissionDefinitionsContext.Provider value={value}>{children}</MissionDefinitionsContext.Provider>
}

export const useMissionDefinitionsContext = () => useContext(MissionDefinitionsContext)
