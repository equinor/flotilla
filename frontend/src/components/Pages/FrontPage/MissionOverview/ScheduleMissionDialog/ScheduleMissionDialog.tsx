import { MissionDefinition } from 'models/MissionDefinition'
import { FetchingMissionsDialog } from './FetchingMissionsDialog'
import { NoMissionsDialog } from './NoMissionsDialog'
import { SelectMissionsToScheduleDialog } from './SelectMissionsToScheduleDialog'

export const ScheduleMissionDialog = ({
    onClose,
    missions,
    isFetchingMissions,
}: {
    onClose: () => void
    missions: MissionDefinition[]
    isFetchingMissions: boolean
}): JSX.Element => {
    const isEmptyMissionsDialogOpen = !isFetchingMissions && missions.length === 0
    const isScheduleMissionDialogOpen = !isFetchingMissions && missions.length !== 0

    return (
        <>
            {isFetchingMissions && <FetchingMissionsDialog closeDialog={onClose} />}
            {isEmptyMissionsDialogOpen && <NoMissionsDialog closeDialog={onClose} />}
            {isScheduleMissionDialogOpen && (
                <SelectMissionsToScheduleDialog missionsList={missions} closeDialog={onClose} />
            )}
        </>
    )
}
