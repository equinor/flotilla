import { createContext, useContext, useState, FC, useEffect } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Robot } from 'models/Robot'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { useLanguageContext } from './LanguageContext'
import { AlertType, useAlertContext } from './AlertContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { Installation } from 'models/Installation'
import { InspectionArea } from 'models/InspectionArea'

const upsertRobotList = (list: Robot[], robot: Robot) => {
    const newList = [...list]
    const i = newList.findIndex((e) => e.id === robot.id)
    if (i > -1) newList[i] = robot
    else newList.push(robot)
    return newList
}

interface Props {
    children: React.ReactNode
}

interface IAssetContext {
    enabledRobots: Robot[]
    installationCode: string
    installationName: string
    installationInspectionAreas: InspectionArea[]
    activeInstallations: Installation[]
    switchInstallation: (selectedName: string) => void
}

const defaultAssetState = {
    enabledRobots: [],
    installationCode: '',
    installationName: '',
    installationInspectionAreas: [],
    activeInstallations: [],
    switchInstallation: () => {},
}

interface RobotPropertyUpdate {
    robotId: string
    propertyName: string
    propertyValue: any
}

export const AssetContext = createContext<IAssetContext>(defaultAssetState)

export const AssetProvider: FC<Props> = ({ children }) => {
    const [enabledRobots, setEnabledRobots] = useState<Robot[]>(defaultAssetState.enabledRobots)
    const [activeInstallations, setActiveInstallations] = useState<Installation[]>([])
    const [selectedInstallation, setSelectedInstallation] = useState<Installation>(
        JSON.parse(window.localStorage.getItem('installation') ?? '{}')
    )
    const [installationInspectionAreas, setInstallationInspectionAreas] = useState<InspectionArea[]>([])

    const { registerEvent, connectionReady } = useSignalRContext()
    const { TranslateText } = useLanguageContext()
    const { setAlert, setListAlert } = useAlertContext()

    const installationCode = selectedInstallation?.installationCode ?? ''
    const installationName = selectedInstallation?.name ?? ''

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.robotAdded, (username: string, message: string) => {
                const updatedRobot: Robot = JSON.parse(message)
                updatedRobot.batteryLevel = undefined
                updatedRobot.batteryState = undefined
                updatedRobot.pressureLevel = undefined
                setEnabledRobots((oldRobotList) => {
                    let oldRobotListCopy = [...oldRobotList]
                    oldRobotListCopy = upsertRobotList(oldRobotListCopy, updatedRobot)
                    return [...oldRobotListCopy]
                })
            })
            registerEvent(SignalREventLabels.robotUpdated, (username: string, message: string) => {
                const updatedRobot: Robot = JSON.parse(message)
                // The check below makes it so that it is not treated as null in the code.
                if (updatedRobot.model.type == null) {
                    console.warn('Received robot update with model type null')
                    return
                }
                setEnabledRobots((oldRobotList) => {
                    let oldRobotListCopy = [...oldRobotList]
                    oldRobotListCopy = upsertRobotList(oldRobotListCopy, updatedRobot)
                    return [...oldRobotListCopy]
                })
            })
            registerEvent(SignalREventLabels.robotPropertyUpdated, (username: string, message: string) => {
                const robotPropertyUpdate: RobotPropertyUpdate = JSON.parse(message)
                setEnabledRobots((oldRobotList) => {
                    const oldRobotListCopy = [...oldRobotList]
                    const index = oldRobotListCopy.findIndex((r) => r.id === robotPropertyUpdate.robotId)
                    if (index > -1) {
                        const robot = oldRobotListCopy[index]
                        if (robotPropertyUpdate.propertyName in robot) {
                            ;(robot as any)[robotPropertyUpdate.propertyName] = robotPropertyUpdate.propertyValue
                            oldRobotListCopy[index] = robot
                        } else {
                            console.warn(`Property ${robotPropertyUpdate.propertyName} does not exist on Robot`)
                        }
                    }
                    return [...oldRobotListCopy]
                })
            })
            registerEvent(SignalREventLabels.robotDeleted, (username: string, message: string) => {
                const updatedRobot: Robot = JSON.parse(message)
                setEnabledRobots((oldRobotList) => {
                    const newRobotList = [...oldRobotList]
                    const index = newRobotList.findIndex((r) => r.id === updatedRobot.id)
                    if (index !== -1) newRobotList.splice(index, 1) // Remove deleted robot
                    return newRobotList
                })
            })
        }
    }, [registerEvent, connectionReady])

    useEffect(() => {
        if (!enabledRobots || enabledRobots.length === 0)
            BackendAPICaller.getEnabledRobots()
                .then((robots) => {
                    robots.forEach((r) => {
                        r.batteryLevel = undefined
                        r.batteryState = undefined
                        r.pressureLevel = undefined
                    })
                    setEnabledRobots(robots)
                })
                .catch(() => {
                    setAlert(
                        AlertType.RequestFail,
                        <FailedRequestAlertContent translatedMessage={TranslateText('Failed to retrieve robots')} />,
                        AlertCategory.ERROR
                    )
                    setListAlert(
                        AlertType.RequestFail,
                        <FailedRequestAlertListContent
                            translatedMessage={TranslateText('Failed to retrieve robots')}
                        />,
                        AlertCategory.ERROR
                    )
                })
    }, [])

    const [filteredRobots, setFilteredRobots] = useState<Robot[]>([])

    useEffect(() => {
        setFilteredRobots(
            enabledRobots.filter(
                (r) => r.currentInstallation.installationCode.toLowerCase() === installationCode.toLowerCase()
            )
        )
    }, [installationCode, enabledRobots])

    useEffect(() => {
        const setOfInstallations: { [id: string]: Installation } = {}
        enabledRobots.forEach((r) => {
            setOfInstallations[r.currentInstallation.id] = r.currentInstallation
        })

        setActiveInstallations(Object.values(setOfInstallations))
    }, [enabledRobots])

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
        const installation = activeInstallations.find((i) => i.name === selectedName)
        if (!installation) return
        setSelectedInstallation(installation)
        window.localStorage.setItem('installation', JSON.stringify(installation))
    }

    const [filteredInstallationInspectionAreas, setFilteredInstallationInspectionAreas] = useState<InspectionArea[]>([])
    useEffect(() => {
        setFilteredInstallationInspectionAreas(
            installationInspectionAreas.filter((d) => d.installationCode === installationCode)
        )
    }, [installationCode, installationInspectionAreas])

    return (
        <AssetContext.Provider
            value={{
                enabledRobots: filteredRobots,
                installationCode,
                installationName,
                installationInspectionAreas: filteredInstallationInspectionAreas,
                activeInstallations: activeInstallations,
                switchInstallation,
            }}
        >
            {children}
        </AssetContext.Provider>
    )
}

export const useAssetContext = () => useContext(AssetContext) as IAssetContext
