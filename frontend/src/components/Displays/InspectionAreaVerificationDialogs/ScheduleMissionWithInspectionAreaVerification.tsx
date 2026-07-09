import { useEffect } from 'react'
import {
    ConflictingMissionInspectionAreasDialog,
    ConflictingRobotInspectionAreaDialog,
} from './ConflictingInspectionAreaDialog'
import { UnknownInspectionAreaDialog } from './UnknownInspectionAreaDialog'
import { useAssetContext } from 'components/Contexts/AssetContext'
import { InspectionArea } from 'models/InspectionArea'

interface IProps {
    scheduleMissions: () => void
    closeDialog: () => void
    robotId: string
    missionInspectionAreas: InspectionArea[]
}

enum DialogTypes {
    unknownNewInspectionArea,
    conflictingMissionInspectionAreas,
    conflictingRobotInspectionArea,
    unknown,
}

export const ScheduleMissionWithInspectionAreaVerification = ({
    robotId,
    missionInspectionAreas,
    scheduleMissions,
    closeDialog,
}: IProps) => {
    const { enabledRobots } = useAssetContext()

    const unikMissionInspectionAreas = missionInspectionAreas.filter(
        (inspectionArea, index, self) => self.findIndex((i) => i.id === inspectionArea.id) === index
    )

    const selectedRobot = enabledRobots.find((robot) => robot.id === robotId)

    const getDialogToOpen = (): DialogTypes | undefined => {
        if (!selectedRobot) return DialogTypes.unknown
        if (unikMissionInspectionAreas.length > 1) return DialogTypes.conflictingMissionInspectionAreas
        if (unikMissionInspectionAreas.length === 0) return DialogTypes.unknownNewInspectionArea
        if (
            selectedRobot.currentInspectionAreaId &&
            unikMissionInspectionAreas[0]?.id !== selectedRobot.currentInspectionAreaId
        ) {
            return DialogTypes.conflictingRobotInspectionArea
        }
        return undefined
    }

    const resolvedDialog = getDialogToOpen()
    const dialogToOpen = resolvedDialog ?? DialogTypes.unknown
    const shouldScheduleDirectly = !!selectedRobot && resolvedDialog === undefined

    useEffect(() => {
        if (shouldScheduleDirectly) scheduleMissions()
    }, [shouldScheduleDirectly])

    const unikMissionInspectionAreaNames = unikMissionInspectionAreas.map((area) => area?.inspectionAreaName ?? '')

    return (
        <>
            {dialogToOpen === DialogTypes.conflictingMissionInspectionAreas && (
                <ConflictingMissionInspectionAreasDialog
                    closeDialog={closeDialog}
                    missionInspectionAreaNames={unikMissionInspectionAreaNames}
                />
            )}
            {dialogToOpen === DialogTypes.conflictingRobotInspectionArea && selectedRobot?.currentInspectionAreaId && (
                <ConflictingRobotInspectionAreaDialog
                    closeDialog={closeDialog}
                    robotInspectionAreaId={selectedRobot?.currentInspectionAreaId}
                    desiredInspectionAreaName={unikMissionInspectionAreaNames![0]}
                />
            )}
            {dialogToOpen === DialogTypes.unknownNewInspectionArea && (
                <UnknownInspectionAreaDialog closeDialog={closeDialog} />
            )}
        </>
    )
}
