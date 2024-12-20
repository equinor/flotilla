import { useState, useEffect } from 'react'
import { InspectionArea } from 'models/InspectionArea'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { MissionDefinition } from 'models/MissionDefinition'
import { ScheduleMissionDialog } from './ScheduleMissionDialogs'
import { getInspectionDeadline } from 'utils/StringFormatting'
import { InspectionTable } from './InspectionTable'
import { StyledDict, compareInspections } from './InspectionUtilities'
import { InspectionAreaCards } from './InspectionAreaCards'
import { Area } from 'models/Area'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { useMissionDefinitionsContext } from 'components/Contexts/MissionDefinitionsContext'

export interface Inspection {
    missionDefinition: MissionDefinition
    deadline: Date | undefined
}

export interface InspectionAreaInspectionTuple {
    areas: Area[]
    inspections: Inspection[]
    inspectionArea: InspectionArea
}

interface InspectionAreaAreaTuple {
    areas: Area[]
    inspectionArea: InspectionArea
}

export const InspectionSection = () => {
    const { ongoingMissions, missionQueue } = useMissionsContext()
    const { installationInspectionAreas, installationAreas } = useInstallationContext()
    const { missionDefinitions } = useMissionDefinitionsContext()
    const [selectedMissions, setSelectedMissions] = useState<MissionDefinition[]>()
    const [isDialogOpen, setIsDialogOpen] = useState<boolean>(false)
    const [isAlreadyScheduled, setIsAlreadyScheduled] = useState<boolean>(false)
    const [selectedInspectionArea, setSelectedInspectionArea] = useState<InspectionArea>()
    const [scrollOnToggle, setScrollOnToggle] = useState<boolean>(true)

    const inspectionAreas: InspectionAreaAreaTuple[] = installationInspectionAreas.map((inspectionArea) => {
        return {
            areas: installationAreas.filter((a) => a.inspectionAreaName === inspectionArea.inspectionAreaName),
            inspectionArea: inspectionArea,
        }
    })

    const inspectionAreaInspections: InspectionAreaInspectionTuple[] =
        inspectionAreas?.map(({ areas, inspectionArea }) => {
            const missionDefinitionsInInspectionArea = missionDefinitions.filter(
                (m) => m.inspectionArea?.inspectionAreaName === inspectionArea.inspectionAreaName
            )
            return {
                inspections: missionDefinitionsInInspectionArea.map((m) => {
                    return {
                        missionDefinition: m,
                        deadline: m.lastSuccessfulRun
                            ? getInspectionDeadline(m.inspectionFrequency, m.lastSuccessfulRun.endTime!)
                            : undefined,
                    }
                }),
                areas: areas,
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
        setIsAlreadyScheduled(false)
        setSelectedMissions([])
        setIsDialogOpen(false)
    }

    const handleScheduleAll = (inspections: Inspection[]) => {
        setIsDialogOpen(true)
        const sortedInspections = inspections.sort(compareInspections)
        setSelectedMissions(sortedInspections.map((i) => i.missionDefinition))
    }

    useEffect(() => {
        if (selectedMissions && selectedMissions.some((mission) => isOngoing(mission) || isScheduled(mission)))
            setIsAlreadyScheduled(true)
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [ongoingMissions, missionQueue, selectedMissions])

    const unscheduledMissions = selectedMissions?.filter((m) => !isOngoing(m) && !isScheduled(m))

    const inspectionArea =
        installationInspectionAreas.length === 1 ? installationInspectionAreas[0] : selectedInspectionArea
    const inspections =
        inspectionAreaInspections.length === 1
            ? inspectionAreaInspections[0].inspections
            : inspectionAreaInspections.find((d) => d.inspectionArea === inspectionArea)?.inspections

    const InspectionAreaSelection = () => (
        <StyledDict.InspectionAreaOverview>
            <InspectionAreaCards
                inspectionAreaMissions={inspectionAreaInspections}
                onClickInspectionArea={onClickInspectionArea}
                selectedInspectionArea={selectedInspectionArea}
                handleScheduleAll={handleScheduleAll}
            />
        </StyledDict.InspectionAreaOverview>
    )

    return (
        <>
            <StyledDict.InspectionAreaOverview>
                {installationInspectionAreas.length !== 1 && <InspectionAreaSelection />}
                {inspectionArea && inspections && (
                    <InspectionTable
                        inspectionArea={inspectionArea}
                        scrollOnToggle={scrollOnToggle}
                        openDialog={() => setIsDialogOpen(true)}
                        setSelectedMissions={setSelectedMissions}
                        inspections={inspections}
                    />
                )}
            </StyledDict.InspectionAreaOverview>
            {isDialogOpen && (
                <ScheduleMissionDialog
                    selectedMissions={selectedMissions!}
                    closeDialog={closeDialog}
                    setMissions={setSelectedMissions}
                    unscheduledMissions={unscheduledMissions!}
                    isAlreadyScheduled={isAlreadyScheduled}
                />
            )}
        </>
    )
}
