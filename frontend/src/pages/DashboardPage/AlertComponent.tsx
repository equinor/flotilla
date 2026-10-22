import { Chip, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useId } from 'react'
import styled from 'styled-components'
import { useLanguageContext } from 'contexts/LanguageContext'
import { AnalysisType } from 'models/MissionDefinition'
import { formatDateTime } from 'utils/StringFormatting'

const analysisDetails: Record<string, { label: string }> = {
    [AnalysisType.CLOE]: { label: 'Constant level oiler' },
    [AnalysisType.Fencilla]: { label: 'Perimeter breach detection' },
    [AnalysisType.ThermalReading]: { label: 'Thermal reading' },
    [AnalysisType.CO2]: { label: 'CO2Measurement' },
}

const maximumRowHeight = '200px'
const minimumRowHeight = '80px'
const minimumIdentityWidth = '160px'
const minimumWarningWidth = '260px'

const StyledRow = styled.div`
    display: flex;
    flex: 0 1 ${maximumRowHeight};
    min-height: ${minimumRowHeight};
    align-items: center;
    gap: ${tokens.spacings.comfortable.xxx_large};
    min-width: 0;
    padding: ${tokens.spacings.comfortable.small} 0;
    padding-left: ${tokens.spacings.comfortable.large};
    border-left: 5px solid ${tokens.colors.interactive.danger__resting.hex};
    border-bottom: 1px dashed ${tokens.colors.ui.background__medium.hex};
    overflow-wrap: anywhere;
    cursor: pointer;

    &:last-child {
        border-bottom: none;
    }
`
const Identity = styled.div`
    display: flex;
    flex-direction: column;
    margin-right: auto;
    min-width: ${minimumIdentityWidth};
    gap: ${tokens.spacings.comfortable.x_small};
`
const TitleRow = styled.div`
    display: flex;
    align-items: center;
    gap: ${tokens.spacings.comfortable.medium};
`
const OrderBadge = styled.span`
    display: flex;
    align-items: center;
    justify-content: center;
    width: 28px;
    height: 28px;
    border-radius: 50%;
    border: 1px solid black;
    font-family: Equinor, sans-serif;
    font-weight: 400;
`
const AnalysisLabel = styled(Typography)`
    font-size: 1.1rem;
`

const AlertImage = styled.img`
    min-width: 0;
    height: 100%;
    aspect-ratio: 16 / 9;
    object-fit: contain;
`
const WarningSection = styled.div`
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    flex-shrink: 0;
    min-width: ${minimumWarningWidth};
    gap: ${tokens.spacings.comfortable.medium};
`
const WarningBadge = styled(Chip)`
    background-color: ${tokens.colors.interactive.danger__highlight.hex};
    color: ${tokens.colors.interactive.danger__text.hex};
    height: 32px;
    font-size: 1rem;
    padding-inline: 16px;
`
interface SaraAlertCardProps {
    analysisType: string
    tag: string
    createdAt: Date
    warning: string
    order: number
    imageSAS?: string
    onClick: () => void
}
export const SaraAlertCard = ({
    analysisType,
    tag,
    createdAt,
    warning,
    order,
    imageSAS,
    onClick,
}: SaraAlertCardProps) => {
    const { TranslateText } = useLanguageContext()
    const titleId = useId()
    if (!warning.trim()) return null
    const details = analysisDetails[analysisType]
    const analysisLabel = TranslateText(details.label)
    return (
        <StyledRow onClick={onClick} aria-labelledby={titleId}>
            <Identity>
                <TitleRow>
                    <Typography id={titleId} as="h3" variant="h4">
                        {tag}
                    </Typography>
                    <OrderBadge>{order}</OrderBadge>
                </TitleRow>
                <AnalysisLabel variant="body_short">{analysisLabel}</AnalysisLabel>
            </Identity>
            {imageSAS && <AlertImage src={imageSAS} alt={analysisLabel} />}
            <WarningSection>
                <WarningBadge>{`${TranslateText('Warning')} - ${warning}`}</WarningBadge>
                <Typography variant="caption">{formatDateTime(createdAt)}</Typography>
            </WarningSection>
        </StyledRow>
    )
}
