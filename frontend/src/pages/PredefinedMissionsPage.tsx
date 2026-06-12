import { InstallationContext } from 'components/Contexts/InstallationContext'
import { Header } from 'components/Header/Header'
import { NavBar } from 'components/Header/NavBar'
import { useContext } from 'react'
import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import styled from 'styled-components'
import { useAlertContext } from 'components/Contexts/AlertContext'
import { StyledPage } from 'components/Styles/StyledComponents'
import { useMissionDefinitionsContext } from 'components/Contexts/MissionDefinitionsContext'
import { phone_width } from 'utils/constants'
import { MissionDefinitionsTable } from './InspectionPage/InspectionTable'
import { Placeholder } from './InspectionPage/InspectionUtilities'

const StyledContent = styled.div`
    display: flex;
    flex-direction: column;
    align-items: end;
    @media (max-width: ${phone_width}) {
        align-items: start;
    }
`
const StyledPlaceholderContent = styled.div`
    width: 70vw;
`
const StyledView = styled.div`
    display: flex;
    align-items: flex-start;
`

export const PredefinedMissionsPage = () => {
    const { alerts } = useAlertContext()
    const { installation } = useContext(InstallationContext)

    const { TranslateText } = useLanguageContext()
    const { missionDefinitions } = useMissionDefinitionsContext()

    return (
        <>
            <Header alertDict={alerts} installation={installation} />
            <NavBar />
            <StyledPage>
                <StyledView>
                    <StyledContent>
                        {missionDefinitions.length > 0 ? (
                            <MissionDefinitionsTable missionDefinitions={missionDefinitions} />
                        ) : (
                            <StyledPlaceholderContent>
                                <Placeholder>
                                    <Typography variant="h4" color="disabled">
                                        {TranslateText('No predefined missions available')}
                                    </Typography>
                                </Placeholder>
                            </StyledPlaceholderContent>
                        )}
                    </StyledContent>
                </StyledView>
            </StyledPage>
        </>
    )
}
