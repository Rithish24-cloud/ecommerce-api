pipeline {
    agent any

    environment {
        IMAGE_NAME = "ecommerce-api"
        CONTAINER_NAME = "ecommerce-api-container"
    }

    stages {
        stage('Checkout') {
            steps {
                git branch: 'main', url: 'https://github.com/Rithish24-cloud/ecommerce-api.git'
            }
        }

        stage('Docker Build') {
            steps {
                sh '''
                docker build -t $IMAGE_NAME:latest .
                '''
            }
        }

        stage('Docker Deploy') {
            steps {
                sh '''
                docker stop $CONTAINER_NAME || true
                docker rm $CONTAINER_NAME || true

                docker run -d \
                --name $CONTAINER_NAME \
                -p 8080:8080 \
                -e ASPNETCORE_ENVIRONMENT=Development \
                $IMAGE_NAME:latest
                '''
            }
        }

        stage('Verify') {
            steps {
                sh '''
                docker ps
                docker logs $CONTAINER_NAME --tail 50
                '''
            }
        }
    }

    post {
        success {
            echo 'Deployment completed successfully.'
        }

        failure {
            echo 'Deployment failed. Check console output.'
        }
    }
}
