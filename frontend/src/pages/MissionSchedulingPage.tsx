import { InstallationContext } from 'contexts/InstallationContext'
import { Header } from 'components/Header/Header'
import { NavBar } from 'components/Header/NavBar'
import { useContext, useState } from 'react'
import { InspectionArea } from 'models/InspectionArea'
import { MissionDefinition } from 'models/MissionDefinition'
import { useMissionsContext } from 'contexts/MissionRunsContext'
import { useMissionDefinitionsContext } from 'contexts/MissionDefinitionsContext'
import { useAssetContext } from 'contexts/AssetContext'
import { PlantPolygonMap } from 'pages/MissionPage/MapPosition/PointillaMapView'
import { Typography } from '@equinor/eds-core-react'
import {
    compareMissionDefinitions,
    InspectionAreaOverview,
    StyledInspectionAreaCards,
} from './MissionSchedulingComponents/InspectionUtilities'
import { InspectionAreaCard } from './MissionSchedulingComponents/InspectionAreaCards'
import { MissionSchedulingTable } from './MissionSchedulingComponents/MissionSchedulingTable'
import { ScheduleMissionDialog } from './MissionSchedulingComponents/ScheduleMissionDialogs'
import { StyledPage } from 'components/Styles/StyledComponents'
import { useLanguageContext } from 'contexts/LanguageContext'

export const MissionSchedulingPage = () => {
    const { installation } = useContext(InstallationContext)
    const { TranslateText } = useLanguageContext()
    const { ongoingMissions, missionQueue } = useMissionsContext()
    const { installationInspectionAreas } = useAssetContext()
    const { missionDefinitions } = useMissionDefinitionsContext()
    const [selectedMissions, setSelectedMissions] = useState<MissionDefinition[]>([])
    const [userSelectedInspectionArea, setSelectedInspectionArea] = useState<InspectionArea | undefined>(undefined)
    const selectedInspectionArea =
        userSelectedInspectionArea ??
        (installationInspectionAreas.length === 1 ? installationInspectionAreas[0] : undefined)

    const countMissionsInInspectionArea = (inspectionArea: InspectionArea) =>
        missionDefinitions.filter((m) => m.inspectionArea.id === inspectionArea.id).length

    const onClickInspectionArea = (clickedInspectionArea: InspectionArea) => {
        setSelectedInspectionArea(clickedInspectionArea)
    }

    const isScheduled = (mission: MissionDefinition) => missionQueue.map((m) => m.missionId).includes(mission.id)
    const isOngoing = (mission: MissionDefinition) => ongoingMissions.map((m) => m.missionId).includes(mission.id)

    const closeDialog = () => {
        setSelectedMissions([])
    }

    const onClickScheduleAll = (inspectionArea: InspectionArea) => {
        const relevantMissionDefinitions = missionDefinitions.filter((m) => m.inspectionArea.id === inspectionArea.id)
        const sortedMissionDefinitions = relevantMissionDefinitions.sort(compareMissionDefinitions)
        setSelectedMissions(sortedMissionDefinitions)
    }

    const isAlreadyScheduled =
        !!selectedMissions && selectedMissions.some((mission) => isOngoing(mission) || isScheduled(mission))

    const unscheduledMissions = selectedMissions?.filter((m) => !isOngoing(m) && !isScheduled(m))

    const selectedInspectionAreaMissionDefinitions = selectedInspectionArea
        ? missionDefinitions.filter((m) => m.inspectionArea.id === selectedInspectionArea.id)
        : undefined

    return (
        <>
            <Header installation={installation} />
            <NavBar />
            <StyledPage>
                <InspectionAreaOverview>
                    {installationInspectionAreas.length > 1 && (
                        <StyledInspectionAreaCards>
                            {installationInspectionAreas.map((inspectionArea) => (
                                <InspectionAreaCard
                                    key={'inspectionAreaCard' + inspectionArea.id}
                                    inspectionArea={inspectionArea}
                                    nMissions={countMissionsInInspectionArea(inspectionArea)}
                                    onClickInspectionArea={onClickInspectionArea}
                                    isSelected={inspectionArea.id === selectedInspectionArea?.id}
                                    isMissionOngoing={ongoingMissions.some(
                                        (m) => m.inspectionArea.id === inspectionArea.id
                                    )}
                                    onClickScheduleAll={onClickScheduleAll}
                                />
                            ))}
                        </StyledInspectionAreaCards>
                    )}
                    {selectedInspectionArea &&
                        selectedInspectionAreaMissionDefinitions &&
                        selectedInspectionAreaMissionDefinitions.length > 0 && (
                            <MissionSchedulingTable
                                inspectionArea={selectedInspectionArea}
                                setSelectedMissions={setSelectedMissions}
                                missionDefinitions={selectedInspectionAreaMissionDefinitions}
                            />
                        )}
                    {selectedInspectionArea &&
                        selectedInspectionAreaMissionDefinitions &&
                        selectedInspectionAreaMissionDefinitions.length === 0 && (
                            <Typography variant="h4" color="disabled">
                                {TranslateText('No missions defined in this area')}
                            </Typography>
                        )}
                    {selectedInspectionArea?.areaPolygon?.positions && (
                        <>
                            {installationInspectionAreas.length === 1 && (
                                <Typography variant="h3" style={{ marginTop: '10px' }}>
                                    {selectedInspectionArea?.inspectionAreaName}
                                </Typography>
                            )}
                            <PlantPolygonMap inspectionArea={selectedInspectionArea} floorId={'0'} />{' '}
                        </>
                    )}
                </InspectionAreaOverview>
                {selectedMissions.length > 0 && (
                    <ScheduleMissionDialog
                        selectedMissions={selectedMissions}
                        closeDialog={closeDialog}
                        unscheduledMissions={unscheduledMissions!}
                        isAlreadyScheduled={isAlreadyScheduled}
                    />
                )}
            </StyledPage>
        </>
    )
}
