import { Button, Icon } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { translateText } from 'components/Contexts/LanguageContext'
import { useNavigate } from 'react-router-dom'
import { Icons } from 'utils/icons'

const StyledButton = styled(Button)`
    width: 100px;
    align-items: baseline;
`

export function BackButton() {
    let navigate = useNavigate()
    return (
        <StyledButton variant="ghost" onClick={() => navigate(-1)}>
            <Icon name={Icons.ArrowBack} size={16} />
            {translateText('Back')}
        </StyledButton>
    )
}
