import { useAlertContext } from 'components/Contexts/AlertContext'
import { InstallationContext } from 'components/Contexts/InstallationContext'
import { Header } from 'components/Header/Header'
import { NavBar } from 'components/Header/NavBar'
import { useContext } from 'react'
import { useState } from 'react'
import { InspectionArea } from 'models/InspectionArea'
import { MissionDefinition } from 'models/MissionDefinition'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { useMissionDefinitionsContext } from 'components/Contexts/MissionDefinitionsContext'
import { useAssetContext } from 'components/Contexts/AssetContext'
import { PlantPolygonMap } from 'pages/MissionPage/MapPosition/PointillaMapView'
import { Typography } from '@equinor/eds-core-react'
import { compareMissionDefinitions, InspectionAreaOverview } from './InspectionPage/InspectionUtilities'
import { InspectionAreaCards } from './InspectionPage/InspectionAreaCards'
import { InspectionTable } from './InspectionPage/InspectionTable'
import { ScheduleMissionDialog } from './InspectionPage/ScheduleMissionDialogs'
import { InspectionAreaInspectionTuple } from './InspectionPage/InspectionSection'
import { StyledPage } from 'components/Styles/StyledComponents'

interface InspectionAreaAreaTuple {
    inspectionArea: InspectionArea
}

export const AreaOverviewPage = () => {
    const { alerts } = useAlertContext()
    const { installation } = useContext(InstallationContext)

    const { ongoingMissions, missionQueue } = useMissionsContext()
    const { installationInspectionAreas } = useAssetContext()
    const { missionDefinitions } = useMissionDefinitionsContext()
    const [selectedMissions, setSelectedMissions] = useState<MissionDefinition[]>()
    const [isDialogOpen, setIsDialogOpen] = useState<boolean>(false)
    const [selectedInspectionArea, setSelectedInspectionArea] = useState<InspectionArea>()
    const [scrollOnToggle, setScrollOnToggle] = useState<boolean>(true)

    const inspectionAreas: InspectionAreaAreaTuple[] = installationInspectionAreas.map((inspectionArea) => {
        return {
            inspectionArea: inspectionArea,
        }
    })

    const inspectionAreaInspections: InspectionAreaInspectionTuple[] =
        inspectionAreas?.map(({ inspectionArea }) => {
            const missionDefinitionsInInspectionArea = missionDefinitions.filter(
                (m) => m.inspectionArea.inspectionAreaName === inspectionArea.inspectionAreaName
            )
            return {
                missionDefinitions: missionDefinitionsInInspectionArea,
                inspectionArea: inspectionArea,
            }
        }) ?? []

    const onClickInspectionArea = (clickedInspectionArea: InspectionArea) => {
        setSelectedInspectionArea(clickedInspectionArea)
        setScrollOnToggle(!scrollOnToggle)
    }

    const isScheduled = (mission: MissionDefinition) => missionQueue.map((m) => m.missionId).includes(mission.id)
    const isOngoing = (mission: MissionDefinition) => ongoingMissions.map((m) => m.missionId).includes(mission.id)

    const closeDialog = () => {
        setSelectedMissions([])
        setIsDialogOpen(false)
    }

    const handleScheduleAll = (missionDefinitions: MissionDefinition[]) => {
        setIsDialogOpen(true)
        const sortedMissionDefinitions = missionDefinitions.sort(compareMissionDefinitions)
        setSelectedMissions(sortedMissionDefinitions)
    }

    const isAlreadyScheduled =
        !!selectedMissions && selectedMissions.some((mission) => isOngoing(mission) || isScheduled(mission))

    const unscheduledMissions = selectedMissions?.filter((m) => !isOngoing(m) && !isScheduled(m))

    const inspectionArea =
        installationInspectionAreas.length === 1 ? installationInspectionAreas[0] : selectedInspectionArea
    const selectedAreaMissionDefinitions =
        inspectionAreaInspections.length === 1
            ? inspectionAreaInspections[0].missionDefinitions
            : inspectionAreaInspections.find((d) => d.inspectionArea === inspectionArea)?.missionDefinitions

    return (
        <>
            <Header alertDict={alerts} installation={installation} />
            <NavBar />
            <StyledPage>
                <InspectionAreaOverview>
                    {installationInspectionAreas.length !== 1 && (
                        <>
                            <InspectionAreaOverview>
                                <InspectionAreaCards
                                    inspectionAreaMissions={inspectionAreaInspections}
                                    onClickInspectionArea={onClickInspectionArea}
                                    selectedInspectionArea={selectedInspectionArea}
                                    handleScheduleAll={handleScheduleAll}
                                />
                            </InspectionAreaOverview>
                            {inspectionArea && selectedAreaMissionDefinitions && (
                                <InspectionTable
                                    inspectionArea={inspectionArea}
                                    scrollOnToggle={scrollOnToggle}
                                    openDialog={() => setIsDialogOpen(true)}
                                    setSelectedMissions={setSelectedMissions}
                                    missionDefinitions={selectedAreaMissionDefinitions}
                                />
                            )}{' '}
                        </>
                    )}
                    {inspectionArea?.plantCode && inspectionArea.areaPolygon?.positions && (
                        <>
                            {installationInspectionAreas.length === 1 && (
                                <Typography variant="h3" style={{ marginTop: '10px' }}>
                                    {inspectionArea?.inspectionAreaName}
                                </Typography>
                            )}
                            <PlantPolygonMap inspectionArea={inspectionArea} floorId={'0'} />{' '}
                        </>
                    )}
                </InspectionAreaOverview>
                {isDialogOpen && (
                    <ScheduleMissionDialog
                        selectedMissions={selectedMissions!}
                        closeDialog={closeDialog}
                        setMissions={setSelectedMissions}
                        unscheduledMissions={unscheduledMissions!}
                        isAlreadyScheduled={isAlreadyScheduled}
                    />
                )}
            </StyledPage>
        </>
    )
}
