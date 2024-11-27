import { Button, Icon } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useNavigate } from 'react-router-dom'
import { config } from 'config'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'

const ContainButton = styled.div`
    display: block;
`

const StyledButton = styled(Button)`
    display: flex;
    align-items: flex-start;
    background-color: ${tokens.colors.ui.background__default.hex};
`

export const MissionHistoryButton = (): JSX.Element => {
    const { TranslateText } = useLanguageContext()

    let navigate = useNavigate()
    const routeChange = () => {
        const path = `${config.FRONTEND_BASE_ROUTE}/history`
        navigate(path)
    }

    return (
        <ContainButton>
            <StyledButton variant="outlined" onClick={routeChange}>
                <Icon name={Icons.Historic} />
                {TranslateText('History')}
            </StyledButton>
        </ContainButton>
    )
}
