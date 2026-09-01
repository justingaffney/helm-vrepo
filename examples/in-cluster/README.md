# In-cluster
Kubernetes manifests for periodically generating a virtual Helm repository index (from the [example configuration](../vrepo.yaml)) and serving it to be accessed from within the cluster.

## Initial generation
The index is periodically generated using a `CronJob`. If an index needs to be generated immediately on deployment run the following command:
```bash
kubectl create job --from=cronjob/helm-vrepo-generator <manual-job-name>
```
