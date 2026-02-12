import { createContext, FC, useContext, useEffect, useState } from 'react'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { MissionDefinition } from 'models/MissionDefinition'
import { useLanguageContext } from './LanguageContext'
import { AlertType, useAlertContext } from './AlertContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { useAssetContext } from './AssetContext'
import { useBackendApi } from 'api/UseBackendApi'

interface IMissionDefinitionsContext {
    missionDefinitions: MissionDefinition[]
}

interface Props {
    children: React.ReactNode
}

const defaultMissionDefinitionsContext: IMissionDefinitionsContext = {
    missionDefinitions: [],
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
    const { installationCode } = useAssetContext()
    const { TranslateText } = useLanguageContext()
    const { setAlert, setListAlert } = useAlertContext()
    const backendApi = useBackendApi()

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.missionDefinitionUpdated, (username: string, message: string) => {
                const missionDefinition: MissionDefinition = JSON.parse(message)
                setMissionDefinitions((oldMissionDefinitions) =>
                    upsertMissionDefinition(oldMissionDefinitions, missionDefinition)
                )
            })
            registerEvent(SignalREventLabels.missionDefinitionCreated, (username: string, message: string) => {
                const missionDefinition: MissionDefinition = JSON.parse(message)
                setMissionDefinitions((oldMissionDefinitions) =>
                    upsertMissionDefinition(oldMissionDefinitions, missionDefinition)
                )
            })
            registerEvent(SignalREventLabels.missionDefinitionDeleted, (username: string, message: string) => {
                const mDef: MissionDefinition = JSON.parse(message)
                setMissionDefinitions((oldMissionDefs) => {
                    const oldListCopy = [...oldMissionDefs]
                    const queueIndex = oldListCopy.findIndex((m) => m.id === mDef.id)
                    if (queueIndex !== -1) oldListCopy.splice(queueIndex, 1) // Remove deleted mission definition
                    return oldListCopy
                })
            })
        }
    }, [registerEvent, connectionReady])

    useEffect(() => {
        const fetchAndUpdateMissionDefinitions = () => {
            backendApi
                .getMissionDefinitions({
                    pageSize: 100,
                    orderBy: 'InstallationCode installationCode',
                })
                .then((response) => {
                    const missionDefinitionsInInstallation = response.content
                    setMissionDefinitions(missionDefinitionsInInstallation ?? [])
                })
                .catch(() => {
                    setAlert(
                        AlertType.RequestFail,
                        <FailedRequestAlertContent
                            translatedMessage={TranslateText('Failed to retrieve inspection plans')}
                        />,
                        AlertCategory.ERROR
                    )
                    setListAlert(
                        AlertType.RequestFail,
                        <FailedRequestAlertListContent
                            translatedMessage={TranslateText('Failed to retrieve inspection plans')}
                        />,
                        AlertCategory.ERROR
                    )
                })
        }
        if (installationCode) fetchAndUpdateMissionDefinitions()
    }, [installationCode])

    const [filteredMissionDefinitions, setFilteredMissionDefinitions] = useState<MissionDefinition[]>([])

    useEffect(() => {
        setFilteredMissionDefinitions(
            missionDefinitions.filter((m) => m.installationCode.toLowerCase() === installationCode.toLowerCase())
        )
    }, [installationCode, missionDefinitions])

    return { missionDefinitions: filteredMissionDefinitions }
}

export const MissionDefinitionsProvider: FC<Props> = ({ children }) => {
    const { missionDefinitions } = useMissionDefinitions()
    return (
        <MissionDefinitionsContext.Provider value={{ missionDefinitions }}>
            {children}
        </MissionDefinitionsContext.Provider>
    )
}

export const useMissionDefinitionsContext = () => useContext(MissionDefinitionsContext)
