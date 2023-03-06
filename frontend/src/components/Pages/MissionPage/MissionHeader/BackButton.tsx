import { Button, Icon } from '@equinor/eds-core-react'
import { arrow_back } from '@equinor/eds-icons'
import styled from 'styled-components'
import { Text } from 'components/Contexts/LanguageContext'
import { useNavigate } from 'react-router-dom'

Icon.add({ arrow_back })

const StyledButton = styled(Button)`
    width: 100px;
    align-items: baseline;
`

export function BackButton() {
    let navigate = useNavigate()
    return (
        <StyledButton variant="ghost" onClick={() => navigate(-1)}>
            <Icon name="arrow_back" size={16} />
            {Text('Back')}
        </StyledButton>
    )
}
