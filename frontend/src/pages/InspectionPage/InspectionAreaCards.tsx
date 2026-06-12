import { InspectionArea } from 'models/InspectionArea'
import { MissionDefinition } from 'models/MissionDefinition'
import { InspectionAreaInspectionTuple } from './InspectionSection'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import {
    CardComponent,
    Content,
    InspectionAreaText,
    Placeholder,
    StyledCard,
    StyledInspectionAreaCard,
    StyledInspectionAreaCards,
    TopInspectionAreaText,
} from './InspectionUtilities'
import { Button, Icon, Tooltip, Typography } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { useAssetContext } from 'components/Contexts/AssetContext'

interface InspectionAreaCardProps {
    inspectionAreaData: InspectionAreaInspectionTuple
    onClickInspectionArea: (inspectionArea: InspectionArea) => void
    selectedInspectionArea: InspectionArea | undefined
    handleScheduleAll: (missionDefinitions: MissionDefinition[]) => void
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

    const isScheduleMissionsDisabled = enabledRobots.length === 0 || inspectionAreaData.missionDefinitions.length === 0

    let queueMissionsTooltip = ''
    if (inspectionAreaData.missionDefinitions.length === 0) queueMissionsTooltip = TranslateText('No available mission')
    else if (isScheduleMissionsDisabled) queueMissionsTooltip = TranslateText('No robot available')

    return (
        <StyledInspectionAreaCard key={inspectionAreaData.inspectionArea.inspectionAreaName}>
            <StyledCard
                key={inspectionAreaData.inspectionArea.inspectionAreaName}
                onClick={
                    inspectionAreaData.missionDefinitions.length > 0
                        ? () => onClickInspectionArea(inspectionAreaData.inspectionArea)
                        : undefined
                }
                style={
                    selectedInspectionArea === inspectionAreaData.inspectionArea
                        ? { border: `solid ${tokens.colors.interactive.focus.hex} 1px` }
                        : {}
                }
            >
                <InspectionAreaText>
                    <TopInspectionAreaText>
                        <Typography variant={'body_short_bold'}>
                            {inspectionAreaData.inspectionArea.inspectionAreaName.toString()}
                        </Typography>
                        {inspectionAreaData.missionDefinitions
                            .filter((m) => ongoingMissions.find((om) => om.missionId === m.id))
                            .map((mission) => (
                                <Content key={mission.id}>
                                    <Icon name={Icons.Ongoing} size={16} />
                                    {TranslateText('InProgress')}
                                </Content>
                            ))}
                    </TopInspectionAreaText>
                    <Typography color={tokens.colors.text.static_icons__secondary.hex}>
                        {inspectionAreaData.missionDefinitions.length}{' '}
                        {inspectionAreaData.missionDefinitions.length === 1
                            ? TranslateText('Mission').toLowerCase()
                            : TranslateText('Missions').toLowerCase()}
                    </Typography>
                </InspectionAreaText>
                <CardComponent>
                    <Tooltip placement="top" title={queueMissionsTooltip}>
                        <Button
                            disabled={isScheduleMissionsDisabled}
                            variant="ghost"
                            onClick={() => handleScheduleAll(inspectionAreaData.missionDefinitions)}
                            color="secondary"
                        >
                            <Icon
                                name={Icons.Add}
                                color={inspectionAreaData.missionDefinitions.length > 0 ? '' : 'grey'}
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

interface IInspectionAreaCardProps {
    inspectionAreaMissions: InspectionAreaInspectionTuple[]
    onClickInspectionArea: (inspectionArea: InspectionArea) => void
    selectedInspectionArea: InspectionArea | undefined
    handleScheduleAll: (missionDefinitions: MissionDefinition[]) => void
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
