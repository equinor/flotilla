import { InspectionArea } from 'models/InspectionArea'
import { useLanguageContext } from 'contexts/LanguageContext'
import {
    CardComponent,
    Content,
    InspectionAreaText,
    StyledCard,
    StyledInspectionAreaCard,
    TopInspectionAreaText,
} from './InspectionUtilities'
import { Button, Icon, Tooltip, Typography } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { useAssetContext } from 'contexts/AssetContext'

interface InspectionAreaCardProps {
    inspectionArea: InspectionArea
    nMissions: number
    onClickInspectionArea: (inspectionArea: InspectionArea) => void
    isSelected: boolean
    isMissionOngoing: boolean
    onClickScheduleAll: (inspectionArea: InspectionArea) => void
}

export const InspectionAreaCard = ({
    inspectionArea,
    nMissions,
    onClickInspectionArea,
    isSelected,
    isMissionOngoing,
    onClickScheduleAll,
}: InspectionAreaCardProps) => {
    const { TranslateText } = useLanguageContext()
    const { enabledRobots } = useAssetContext()

    const isScheduleMissionsDisabled = enabledRobots.length === 0 || nMissions === 0

    let queueMissionsTooltip = ''
    if (nMissions === 0) queueMissionsTooltip = TranslateText('No available mission')
    else if (isScheduleMissionsDisabled) queueMissionsTooltip = TranslateText('No robot available')

    return (
        <StyledInspectionAreaCard key={inspectionArea.inspectionAreaName}>
            <StyledCard
                key={inspectionArea.inspectionAreaName}
                onClick={() => onClickInspectionArea(inspectionArea)}
                style={isSelected ? { border: `solid ${tokens.colors.interactive.focus.hex} 1px` } : {}}
            >
                <InspectionAreaText>
                    <TopInspectionAreaText>
                        <Typography variant={'body_short_bold'}>
                            {inspectionArea.inspectionAreaName.toString()}
                        </Typography>
                        {isMissionOngoing && (
                            <Content>
                                <Icon name={Icons.Ongoing} size={16} />
                                <Typography>{TranslateText('InProgress')}</Typography>
                            </Content>
                        )}
                    </TopInspectionAreaText>
                    <Typography color={tokens.colors.text.static_icons__secondary.hex}>
                        {nMissions}{' '}
                        {nMissions === 1
                            ? TranslateText('Mission').toLowerCase()
                            : TranslateText('Missions').toLowerCase()}
                    </Typography>
                </InspectionAreaText>
                <CardComponent>
                    <Tooltip placement="top" title={queueMissionsTooltip}>
                        <Button
                            disabled={isScheduleMissionsDisabled}
                            variant="ghost"
                            onClick={() => onClickScheduleAll(inspectionArea)}
                            color="secondary"
                        >
                            <Icon name={Icons.Add} color={nMissions > 0 ? '' : 'grey'} />
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
