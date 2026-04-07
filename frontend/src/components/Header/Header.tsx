import { Button, Icon, TopBar, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { SelectLanguage } from 'components/Header/LanguageSelector'
import { Icons } from 'utils/icons'
import { AlertDictionaryType } from 'components/Contexts/AlertContext'
import { AlertBanner } from 'components/Alerts/AlertsBanner'
import { FrontPageSectionId } from 'models/FrontPageSectionId'
import { AlertIcon } from 'components/Header/AlertIcon'
import { useNavigate } from 'react-router-dom'
import { phone_width } from 'utils/constants'
import { Installation } from 'models/Installation'

const StyledTopBar = styled(TopBar)`
    align-items: center;
    box-shadow: none;
    @media (max-width: ${phone_width}) {
        grid-column-gap: 12px;
    }
    height: fit-content;
`
const StyledWrapper = styled.div`
    display: flex;
    flex-direction: row;
    align-items: center;
`
const IconStyle = styled.div`
    display: flex;
    align-items: center;
    flex-direction: row-reverse;
    gap: 0.8rem;
`
const HandPointer = styled.div`
    cursor: pointer;
`
const SelectLanguageWrapper = styled.div`
    margin-left: 1rem;
`
const StyledAlertList = styled.div`
    display: grid;
    grid-template-rows: repeat(auto-fill);
    align-items: center;
    gap: 0.5rem;
`
const StyledTopBarHeader = styled.div`
    display: flex;
    flex-direction: row;
    gap: 4px;
`

interface Props {
    alertDict?: AlertDictionaryType
    installation?: Installation
}

export const Header = ({ alertDict, installation }: Props) => {
    const navigate = useNavigate()

    return (
        <>
            <StyledTopBar id={FrontPageSectionId.TopBar}>
                <StyledWrapper>
                    <TopBar.Header onClick={() => navigate(`/${installation?.installationCode || ''}`)}>
                        <StyledTopBarHeader>
                            <HandPointer>
                                <Typography variant="body_short_bold" color="text-primary">
                                    Flotilla
                                </Typography>
                            </HandPointer>
                            {installation && (
                                <>
                                    <Typography variant="body_short" color="text-primary">
                                        {`| ${installation.name}`}
                                    </Typography>
                                </>
                            )}
                        </StyledTopBarHeader>
                    </TopBar.Header>
                </StyledWrapper>
                <TopBar.Actions>
                    <IconStyle>
                        {alertDict && <AlertIcon />}
                        <Button variant="ghost_icon" onClick={() => navigate(`/`)}>
                            <Icon name={Icons.Platform} size={24} title="Change Asset" />
                        </Button>
                        <Button variant="ghost_icon" onClick={() => navigate(`/info`)}>
                            <Icon name={Icons.Info} size={24} title="Info Page" />
                        </Button>
                    </IconStyle>
                    <SelectLanguageWrapper>
                        <SelectLanguage />
                    </SelectLanguageWrapper>
                </TopBar.Actions>
            </StyledTopBar>
            {alertDict && Object.entries(alertDict).length > 0 && (
                <StyledAlertList>
                    {Object.entries(alertDict).map(([key, value]) => (
                        <AlertBanner key={key} dismissAlert={value.dismissFunction} alertCategory={value.alertCategory}>
                            {value.content}
                        </AlertBanner>
                    ))}
                </StyledAlertList>
            )}
        </>
    )
}
