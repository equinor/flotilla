import { InspectionArea } from 'models/InspectionArea'
import { InspectionAreaInspectionTuple, Inspection } from './InspectionSection'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import {
    CardMissionInformation,
    InspectionAreaCardColors,
    StyledDict,
    compareInspections,
    getDeadlineInspection,
} from './InspectionUtilities'
import { Button, Icon, Tooltip, Typography } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { useRobotContext } from 'components/Contexts/RobotContext'

interface IInspectionAreaCardProps {
    inspectionAreaMissions: InspectionAreaInspectionTuple[]
    onClickInspectionArea: (inspectionArea: InspectionArea) => void
    selectedInspectionArea: InspectionArea | undefined
    handleScheduleAll: (inspections: Inspection[]) => void
}

interface InspectionAreaCardProps {
    inspectionAreaData: InspectionAreaInspectionTuple
    onClickInspectionArea: (inspectionArea: InspectionArea) => void
    selectedInspectionArea: InspectionArea | undefined
    handleScheduleAll: (inspections: Inspection[]) => void
}

const InspectionAreaCard = ({
    inspectionAreaData,
    onClickInspectionArea,
    selectedInspectionArea,
    handleScheduleAll,
}: InspectionAreaCardProps) => {
    const { TranslateText } = useLanguageContext()
    const { ongoingMissions } = useMissionsContext()
    const { enabledRobots } = useRobotContext()

    const isScheduleMissionsDisabled = enabledRobots.length === 0 || inspectionAreaData.inspections.length === 0

    const getCardColorFromInspections = (inspections: Inspection[]): InspectionAreaCardColors => {
        if (inspections.length === 0) return InspectionAreaCardColors.Gray
        const sortedInspections = inspections.sort(compareInspections)

        if (sortedInspections.length === 0) return InspectionAreaCardColors.Green

        const nextInspection = sortedInspections[0]
        if (!nextInspection.deadline) {
            if (!nextInspection.missionDefinition.inspectionFrequency) return InspectionAreaCardColors.Green
            else return InspectionAreaCardColors.Red
        }

        return getDeadlineInspection(nextInspection.deadline)
    }

    let queueMissionsTooltip = ''
    if (inspectionAreaData.inspections.length === 0) queueMissionsTooltip = TranslateText('No planned inspection')
    else if (isScheduleMissionsDisabled) queueMissionsTooltip = TranslateText('No robot available')

    return (
        <StyledDict.InspectionAreaCard key={inspectionAreaData.inspectionArea.inspectionAreaName}>
            <StyledDict.Rectangle
                style={{ background: `${getCardColorFromInspections(inspectionAreaData.inspections)}` }}
            />
            <StyledDict.Card
                key={inspectionAreaData.inspectionArea.inspectionAreaName}
                onClick={
                    inspectionAreaData.inspections.length > 0
                        ? () => onClickInspectionArea(inspectionAreaData.inspectionArea)
                        : undefined
                }
                style={
                    selectedInspectionArea === inspectionAreaData.inspectionArea
                        ? { border: `solid ${getCardColorFromInspections(inspectionAreaData.inspections)} 2px` }
                        : {}
                }
            >
                <StyledDict.InspectionAreaText>
                    <StyledDict.TopInspectionAreaText>
                        <Typography variant={'body_short_bold'}>
                            {inspectionAreaData.inspectionArea.inspectionAreaName.toString()}
                        </Typography>
                        {inspectionAreaData.inspections
                            .filter((i) => ongoingMissions.find((m) => m.missionId === i.missionDefinition.id))
                            .map((inspection) => (
                                <StyledDict.Content key={inspection.missionDefinition.id}>
                                    <Icon name={Icons.Ongoing} size={16} />
                                    {TranslateText('InProgress')}
                                </StyledDict.Content>
                            ))}
                    </StyledDict.TopInspectionAreaText>
                    {inspectionAreaData.inspections && (
                        <CardMissionInformation
                            inspectionAreaName={inspectionAreaData.inspectionArea.inspectionAreaName}
                            inspections={inspectionAreaData.inspections}
                        />
                    )}
                </StyledDict.InspectionAreaText>
                <StyledDict.CardComponent>
                    <Tooltip placement="top" title={queueMissionsTooltip}>
                        <Button
                            disabled={isScheduleMissionsDisabled}
                            variant="outlined"
                            onClick={() => handleScheduleAll(inspectionAreaData.inspections)}
                            color="secondary"
                        >
                            <Icon
                                name={Icons.LibraryAdd}
                                color={inspectionAreaData.inspections.length > 0 ? '' : 'grey'}
                            />
                            <Typography color={tokens.colors.text.static_icons__secondary.rgba}>
                                {TranslateText('Queue the missions')}
                            </Typography>
                        </Button>
                    </Tooltip>
                </StyledDict.CardComponent>
            </StyledDict.Card>
        </StyledDict.InspectionAreaCard>
    )
}

export const InspectionAreaCards = ({
    inspectionAreaMissions,
    onClickInspectionArea,
    selectedInspectionArea,
    handleScheduleAll,
}: IInspectionAreaCardProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <>
            {Object.keys(inspectionAreaMissions).length > 0 ? (
                <StyledDict.InspectionAreaCards>
                    {inspectionAreaMissions.map((inspectionAreaMission) => (
                        <InspectionAreaCard
                            key={'inspectionAreaCard' + inspectionAreaMission.inspectionArea.inspectionAreaName}
                            inspectionAreaData={inspectionAreaMission}
                            onClickInspectionArea={onClickInspectionArea}
                            selectedInspectionArea={selectedInspectionArea}
                            handleScheduleAll={handleScheduleAll}
                        />
                    ))}
                </StyledDict.InspectionAreaCards>
            ) : (
                <StyledDict.Placeholder>
                    <Typography variant="h4" color="disabled">
                        {TranslateText('No inspections available')}
                    </Typography>
                </StyledDict.Placeholder>
            )}
        </>
    )
}
