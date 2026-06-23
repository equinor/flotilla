import { useAlertContext } from 'components/Contexts/AlertContext'
import { InstallationContext } from 'components/Contexts/InstallationContext'
import { Header } from 'components/Header/Header'
import { NavBar } from 'components/Header/NavBar'
import { useContext } from 'react'
import { cardShadow, StyledPage } from 'components/Styles/StyledComponents'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Card, Typography, Icon } from '@equinor/eds-core-react'
import { styled } from 'styled-components'
import { useNavigate } from 'react-router-dom'
import { StyledGhostButton } from './FrontPage/MissionOverview/RobotCard'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import cloe from 'mediaAssets/cloe.png'
import fenceBreach from 'mediaAssets/fenceBreach.png'
import thermalReading from 'mediaAssets/thermalReading.png'

const StyledCard = styled(Card)`
    width: clamp(600px, 50%, 700px);
    padding: 1.25rem;
    box-shadow: ${cardShadow};
    border-left: 4px solid ${tokens.colors.interactive.warning__hover.hex};

    @media (max-width: 960px) {
        width: 90%;
    }
`
const DataCardImage = styled.img`
    width: 100px;
    height: 100px;
    object-fit: contain;
    margin-bottom: 8px;
`
const DataCardRow = styled.div`
    display: flex;
    align-items: center;
    gap: 20px;

    @media (max-width: 960px) {
        flex-direction: column;
        align-items: flex-start;
    }
`
const DataCardContent = styled.div`
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
`

export const DataOverviewPage = () => {
    const { alerts } = useAlertContext()
    const { installation } = useContext(InstallationContext)
    const { TranslateText } = useLanguageContext()
    const navigate = useNavigate()

    const analysisPages = [
        ...(installation.installationCode?.toUpperCase() === 'NLS'
            ? [
                  {
                      to: `/${installation.installationCode}/cloe-view`,
                      label: TranslateText('Data View for Constant Level Oilers'),
                      description: TranslateText('Analysis performed on images of the constant level oilers'),
                      source: cloe,
                  },
              ]
            : []),
        ...(installation.installationCode?.toUpperCase() === 'NLS'
            ? [
                  {
                      to: `/${installation.installationCode}/fencilla-view`,
                      label: TranslateText('Data View for Perimeter Breach Detection'),
                      description: TranslateText('Analysis performed on images of the perimeter fences'),
                      source: fenceBreach,
                  },
              ]
            : []),
        ...(installation.installationCode?.toUpperCase() === 'KAA'
            ? [
                  {
                      to: `/${installation.installationCode}/thermal-reading-view`,
                      label: TranslateText('Data View for Thermal Reading for Pumps'),
                      description: TranslateText('Analysis performed on thermal images of the pumps'),
                      source: thermalReading,
                  },
              ]
            : []),
    ]

    return (
        <>
            <Header alertDict={alerts} installation={installation} />
            <NavBar />
            <StyledPage>
                {analysisPages.map((page) => (
                    <StyledCard key={page.to}>
                        <DataCardRow>
                            <DataCardImage src={page.source} alt={page.label} />
                            <DataCardContent>
                                <Typography variant="h4">{page.label}</Typography>
                                <Typography variant="body_short">{page.description}</Typography>
                                <StyledGhostButton
                                    variant="ghost"
                                    style={{ color: tokens.colors.interactive.warning__hover.hex }}
                                    onClick={() => navigate(page.to)}
                                >
                                    {TranslateText('Go to Data View')}
                                    <Icon name={Icons.RightCheveron} size={16} />
                                </StyledGhostButton>
                            </DataCardContent>
                        </DataCardRow>
                    </StyledCard>
                ))}
            </StyledPage>
        </>
    )
}
