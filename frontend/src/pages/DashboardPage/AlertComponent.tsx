import { Card, Chip, Icon, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useId } from 'react'
import styled from 'styled-components'
import { AnalysisValueDisplay } from 'components/Displays/TaskDisplay'
import { useLanguageContext } from 'contexts/LanguageContext'
import { AnalysisType } from 'models/MissionDefinition'
import cloe from 'mediaAssets/cloe.png'
import fenceBreach from 'mediaAssets/fenceBreach.png'
import thermalReading from 'mediaAssets/thermalReading.png'
import { formatDateTime } from 'utils/StringFormatting'
import { Icons } from 'utils/icons'
const analysisDetails: Record<string, { label: string; valueLabel: string; image?: string }> = {
    [AnalysisType.CLOE]: { label: 'Constant level oiler', valueLabel: 'Fill level', image: cloe },
    [AnalysisType.Fencilla]: { label: 'Perimeter breach detection', valueLabel: 'Value', image: fenceBreach },
    [AnalysisType.ThermalReading]: { label: 'Thermal reading', valueLabel: 'Temperature', image: thermalReading },
    [AnalysisType.CO2]: { label: 'CO2Measurement', valueLabel: 'CO2Measurement' },
}
const StyledCard = styled(Card)`
    min-width: 0;
    border-left: ${tokens.spacings.comfortable.x_small} solid ${tokens.colors.interactive.warning__resting.hex};
    overflow-wrap: anywhere;
`
const CardContent = styled(Card.Content)`
    display: flex;
    align-items: center;
    padding-top: ${tokens.spacings.comfortable.medium};
    gap: ${tokens.spacings.comfortable.xxx_large};
`
const Identity = styled.div`
    display: flex;
    flex: 2 1 230px;
    min-width: 0;
    align-items: flex-start;
    gap: ${tokens.spacings.comfortable.medium};
    svg {
        flex-shrink: 0;
        margin-top: ${tokens.spacings.comfortable.x_small};
    }
`
const TagDetails = styled(Card.HeaderTitle)`
    min-width: 0;
    gap: ${tokens.spacings.comfortable.x_small};
`
const WarningBadge = styled(Chip)`
    background-color: ${tokens.colors.interactive.warning__highlight.hex};
    color: ${tokens.colors.interactive.warning__hover.hex};
`
const Reading = styled.div`
    flex: 1 1 160px;
    min-width: 0;
`
const WarningText = styled(Typography)`
    white-space: pre-wrap;
`
const Media = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
    flex: 0 1 128px;
    min-width: 0;
    gap: ${tokens.spacings.comfortable.x_small};
`
const AnalysisImage = styled.img`
    width: 100px;
    height: 80px;
    object-fit: contain;
`
interface SaraAlertCardProps {
    analysisType: string
    tag: string
    area?: string
    createdAt: Date
    value: string
    unit: string
    warning: string
}
export const SaraAlertCard = ({ analysisType, tag, area, createdAt, value, unit, warning }: SaraAlertCardProps) => {
    const { TranslateText } = useLanguageContext()
    const titleId = useId()
    const valueLabelId = useId()
    if (!warning.trim()) return null
    const details = analysisDetails[analysisType]
    const analysisLabel = TranslateText(details.label)
    return (
        <StyledCard role="article" aria-labelledby={titleId} elevation="raised">
            <CardContent>
                <Identity>
                    <Icon
                        name={Icons.Warning}
                        color={tokens.colors.interactive.warning__hover.hex}
                        aria-hidden="true"
                    />
                    <TagDetails>
                        <Typography id={titleId} as="h3" variant="h3">
                            {tag}
                        </Typography>
                        <Typography variant="body_short">{analysisLabel}</Typography>
                        {area?.trim() && (
                            <Typography variant="body_short">
                                {TranslateText('Area')} {area}
                            </Typography>
                        )}
                        <WarningBadge>{`${TranslateText('Warning')} - ${warning}`}</WarningBadge>
                    </TagDetails>
                </Identity>
                <Reading>
                    <Typography id={valueLabelId} variant="overline">
                        {TranslateText(details.valueLabel)}
                    </Typography>
                    <div role="group" aria-labelledby={valueLabelId}>
                        <AnalysisValueDisplay value={value} unit={unit} analysisType={analysisType} />
                    </div>
                    <WarningText variant="body_short">{warning}</WarningText>
                </Reading>
                <Media>
                    {details.image && <AnalysisImage src={details.image} alt={analysisLabel} />}
                    <Typography variant="caption">{formatDateTime(createdAt)}</Typography>
                </Media>
            </CardContent>
        </StyledCard>
    )
}
