import { InspectionArea } from 'models/InspectionArea'
import { InspectionAreaInspectionTuple, Inspection } from './InspectionSection'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import {
    CardComponent,
    CardMissionInformation,
    Content,
    InspectionAreaCardColors,
    InspectionAreaText,
    Placeholder,
    Rectangle,
    StyledCard,
    StyledInspectionAreaCard,
    StyledInspectionAreaCards,
    TopInspectionAreaText,
    compareInspections,
    getDeadlineInspection,
} from './InspectionUtilities'
import { Button, Icon, Tooltip, Typography } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { useAssetContext } from 'components/Contexts/AssetContext'

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
    const { enabledRobots } = useAssetContext()

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
        <StyledInspectionAreaCard key={inspectionAreaData.inspectionArea.inspectionAreaName}>
            <Rectangle style={{ background: `${getCardColorFromInspections(inspectionAreaData.inspections)}` }} />
            <StyledCard
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
                <InspectionAreaText>
                    <TopInspectionAreaText>
                        <Typography variant={'body_short_bold'}>
                            {inspectionAreaData.inspectionArea.inspectionAreaName.toString()}
                        </Typography>
                        {inspectionAreaData.inspections
                            .filter((i) => ongoingMissions.find((m) => m.missionId === i.missionDefinition.id))
                            .map((inspection) => (
                                <Content key={inspection.missionDefinition.id}>
                                    <Icon name={Icons.Ongoing} size={16} />
                                    {TranslateText('InProgress')}
                                </Content>
                            ))}
                    </TopInspectionAreaText>
                    {inspectionAreaData.inspections && (
                        <CardMissionInformation
                            inspectionAreaName={inspectionAreaData.inspectionArea.inspectionAreaName}
                            inspections={inspectionAreaData.inspections}
                        />
                    )}
                </InspectionAreaText>
                <CardComponent>
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
                            <Typography color={tokens.colors.text.static_icons__secondary.hex}>
                                {TranslateText('Queue the missions')}
                            </Typography>
                        </Button>
                    </Tooltip>
                </CardComponent>
            </StyledCard>
        </StyledInspectionAreaCard>
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
                <StyledInspectionAreaCards>
                    {inspectionAreaMissions.map((inspectionAreaMission) => (
                        <InspectionAreaCard
                            key={'inspectionAreaCard' + inspectionAreaMission.inspectionArea.inspectionAreaName}
                            inspectionAreaData={inspectionAreaMission}
                            onClickInspectionArea={onClickInspectionArea}
                            selectedInspectionArea={selectedInspectionArea}
                            handleScheduleAll={handleScheduleAll}
                        />
                    ))}
                </StyledInspectionAreaCards>
            ) : (
                <Placeholder>
                    <Typography variant="h4" color="disabled">
                        {TranslateText('No inspections available')}
                    </Typography>
                </Placeholder>
            )}
        </>
    )
}
