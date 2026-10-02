import { Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { useLanguageContext } from 'contexts/LanguageContext'
import { ContentCard } from 'components/Styles/StyledComponents'
import { InspectionData } from 'models/InspectionRecord'
import { SaraAlertCard } from './AlertComponent'

const AlertPanelCard = styled(ContentCard)`
    flex: 1;
    min-height: 0;
    overflow-y: auto;
`

const AlertList = styled.div`
    display: flex;
    flex-direction: column;
    flex: 1;
    min-height: 100%;
    gap: 16px;
`

const PlaceholderMessage = styled.div`
    display: flex;
    flex: 1 0 80px;
    align-items: center;
    justify-content: center;
    border: 1px dashed ${tokens.colors.ui.background__medium.hex};
    border-radius: 4px;
    color: ${tokens.colors.text.static_icons__tertiary.hex};
    text-align: center;
    padding: 16px;
`

interface DashboardAlertPanelProps {
    alerts: InspectionData[]
}

export const DashboardAlertPanel = ({ alerts }: DashboardAlertPanelProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <AlertPanelCard>
            {alerts.length === 0 ? (
                <PlaceholderMessage>
                    <Typography variant="body_short">{TranslateText('There are no alert messages here')}</Typography>
                </PlaceholderMessage>
            ) : (
                <AlertList>
                    {alerts.map((alert, index) => (
                        <SaraAlertCard
                            key={alert.analysisId}
                            order={index + 1}
                            analysisType={alert.analysisType!}
                            tag={alert.tag!}
                            createdAt={alert.createdAt!}
                            warning={alert.warning!}
                        />
                    ))}
                    <PlaceholderMessage>
                        <Typography variant="body_short">
                            {TranslateText('There are no further alerts that need follow-up')}
                        </Typography>
                    </PlaceholderMessage>
                </AlertList>
            )}
        </AlertPanelCard>
    )
}
