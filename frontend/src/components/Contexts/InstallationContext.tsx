import { createContext, FC, useContext, useState, useEffect } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { PlantInfo } from 'models/MissionDefinition'
import { InspectionArea } from 'models/InspectionArea'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { useLanguageContext } from './LanguageContext'
import { AlertType, useAlertContext } from './AlertContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'

interface IInstallationContext {
    installationCode: string
    installationName: string
    installationInspectionAreas: InspectionArea[]
    switchInstallation: (selectedName: string) => void
}

const mapInstallationCodeToName = (plantInfoArray: PlantInfo[]): Map<string, string> => {
    const mapping = new Map<string, string>()
    plantInfoArray.forEach((plantInfo: PlantInfo) => {
        mapping.set(plantInfo.projectDescription, plantInfo.plantCode)
    })
    return mapping
}

interface Props {
    children: React.ReactNode
}

const defaultInstallation = {
    installationCode: '',
    installationName: '',
    installationInspectionAreas: [],
    switchInstallation: () => {},
}

export const InstallationContext = createContext<IInstallationContext>(defaultInstallation)

export const InstallationProvider: FC<Props> = ({ children }) => {
    const { registerEvent, connectionReady } = useSignalRContext()
    const { TranslateText } = useLanguageContext()
    const { setAlert, setListAlert } = useAlertContext()
    const [allPlantsMap, setAllPlantsMap] = useState<Map<string, string>>(new Map())
    const [installationName, setInstallationName] = useState<string>(
        window.localStorage.getItem('installationName') || ''
    )
    const [installationInspectionAreas, setInstallationInspectionAreas] = useState<InspectionArea[]>([])

    const installationCode = (allPlantsMap.get(installationName) || '').toUpperCase()

    useEffect(() => {
        BackendAPICaller.getPlantInfo()
            .then((response: PlantInfo[]) => {
                const mapping = mapInstallationCodeToName(response)
                setAllPlantsMap(mapping)
            })
            .catch(() => {
                setAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertContent translatedMessage={TranslateText('Failed to retrieve installations')} />,
                    AlertCategory.ERROR
                )
                setListAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertListContent
                        translatedMessage={TranslateText('Failed to retrieve installations from Echo')}
                    />,
                    AlertCategory.ERROR
                )
            })
    }, [])

    useEffect(() => {
        if (installationCode)
            BackendAPICaller.getInspectionAreasByInstallationCode(installationCode)
                .then((inspectionAreas: InspectionArea[]) => {
                    setInstallationInspectionAreas(inspectionAreas)
                })
                .catch(() => {
                    setAlert(
                        AlertType.RequestFail,
                        <FailedRequestAlertContent
                            translatedMessage={TranslateText(
                                'Failed to retrieve inspection areas on installation {0}',
                                [installationCode]
                            )}
                        />,
                        AlertCategory.ERROR
                    )
                    setListAlert(
                        AlertType.RequestFail,
                        <FailedRequestAlertListContent
                            translatedMessage={TranslateText(
                                'Failed to retrieve inspection areas on installation {0}',
                                [installationCode]
                            )}
                        />,
                        AlertCategory.ERROR
                    )
                })
    }, [installationCode])

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.inspectionAreaCreated, (username: string, message: string) => {
                const newInspectionArea: InspectionArea = JSON.parse(message)
                if (newInspectionArea.installationCode !== installationCode) return
                setInstallationInspectionAreas((oldInspectionAreas) => {
                    return [...oldInspectionAreas, newInspectionArea]
                })
            })
            registerEvent(SignalREventLabels.inspectionAreaUpdated, (username: string, message: string) => {
                const updatedInspectionArea: InspectionArea = JSON.parse(message)
                if (updatedInspectionArea.installationCode !== installationCode) return

                setInstallationInspectionAreas((oldInspectionAreas) => {
                    const inspectionAreaIndex = oldInspectionAreas.findIndex((d) => d.id === updatedInspectionArea.id)
                    if (inspectionAreaIndex === -1) return [...oldInspectionAreas, updatedInspectionArea]
                    else {
                        const oldInspectionAreasCopy = [...oldInspectionAreas]
                        oldInspectionAreasCopy[inspectionAreaIndex] = updatedInspectionArea
                        return oldInspectionAreasCopy
                    }
                })
            })
            registerEvent(SignalREventLabels.inspectionAreaDeleted, (username: string, message: string) => {
                const deletedInspectionArea: InspectionArea = JSON.parse(message)
                if (deletedInspectionArea.installationCode !== installationCode) return
                setInstallationInspectionAreas((oldInspectionAreas) => {
                    const inspectionAreaIndex = oldInspectionAreas.findIndex((d) => d.id === deletedInspectionArea.id)
                    if (inspectionAreaIndex !== -1) {
                        const oldInspectionAreasCopy = [...oldInspectionAreas]
                        oldInspectionAreasCopy.splice(inspectionAreaIndex, 1)
                        return oldInspectionAreasCopy
                    }
                    return oldInspectionAreas
                })
            })
        }
    }, [registerEvent, connectionReady])

    const switchInstallation = (selectedName: string) => {
        setInstallationName(selectedName)
        window.localStorage.setItem('installationName', selectedName)
        const derivedCode = allPlantsMap.get(selectedName) || ''
        window.localStorage.setItem('installationCode', derivedCode)
    }

    const [filteredInstallationInspectionAreas, setFilteredInstallationInspectionAreas] = useState<InspectionArea[]>([])
    useEffect(() => {
        setFilteredInstallationInspectionAreas(
            installationInspectionAreas.filter((d) => d.installationCode === installationCode)
        )
    }, [installationCode, installationInspectionAreas])

    return (
        <InstallationContext.Provider
            value={{
                installationCode,
                installationName,
                installationInspectionAreas: filteredInstallationInspectionAreas,
                switchInstallation,
            }}
        >
            {children}
        </InstallationContext.Provider>
    )
}

export const useInstallationContext = () => useContext(InstallationContext)
